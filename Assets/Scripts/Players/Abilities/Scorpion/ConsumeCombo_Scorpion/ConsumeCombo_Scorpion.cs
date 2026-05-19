using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class ConsumeCombo_Scorpion : Skill
{
    [SerializeField]private ComboPoints_Player _comboPoints;
    
    private List<Character> _comboTargetsQueue = new List<Character>();

    [SyncVar]
    private uint _lastCharacterNetId;
    
    public Character LastCharacterNet
    {
        get
        {
            if (_lastCharacterNetId == 0)
                return null;

            if (NetworkClient.spawned.TryGetValue(_lastCharacterNetId, out var identity))
                return identity.GetComponent<Character>();

            return null;
        }
    }
    
    private CharacterState _lastCharacterState;
    public CharacterState LastCharacterState
    {
        get => _lastCharacterState;
        set => _lastCharacterState = value;
    }

    protected override bool IsCanCast => true;
    
    private bool IsEnemyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Enemy");
    private bool _ninjaTalentEnabled = false;
    private bool _fireComboTalentEnabled = false; 
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    private bool isConsumeCombo_ScorpionPhysicStateClear;
    public bool IsConsumeCombo_ScorpionPhysicStateClear => isConsumeCombo_ScorpionPhysicStateClear;
    private bool _canCastUnderPhysicalDisable = false;
    
    #region 3% heal per dispelled physical effect
    private ConsumeComboHealOnDispelBooster _healOnDispelBooster;
    public ConsumeComboHealOnDispelBooster HealOnDispelBooster => _healOnDispelBooster;
    #endregion
    
    #region 5% energy per dispelled physical effect
    private ConsumeComboEnergyOnDispelBooster _energyOnDispelBooster;
    public ConsumeComboEnergyOnDispelBooster EnergyOnDispelBooster => _energyOnDispelBooster;
    #endregion

    #region +1 stacks combo count
    private bool _isComboStacksIncreased;
    private int _newMaxStackCount = 4;
    #endregion
    
    private float _clickRadius = 0.5f;
    
    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);
        _comboPoints = hero.GetComponent<ComboPoints_Player>();
    }

    private void OnEnable()
    {
        _healOnDispelBooster = new ConsumeComboHealOnDispelBooster(this);
        _energyOnDispelBooster = new ConsumeComboEnergyOnDispelBooster(this);
    }

    public void OnComboStacksIncreased(bool value)
    {
        if(_isComboStacksIncreased == value) return;
        
        _isComboStacksIncreased = value;

        if (!value)
        {
            foreach (var target in GetTargetWithCombo())
            {
                var state = target.GetComponent<CharacterState>().GetState(States.ComboState) as ComboState;
                if (state == null) return;

                state.MaxStacksCount = state.InitialStackCount;
                if(state.CurrentStacksCount > state.InitialStackCount)
                    state.ReduceStack();
                
                if(isClient)
                    CmdDecreaseStacksOnTargets(target);
            }
        }
        else
        {
            foreach (var target in GetTargetWithCombo())
            {
                var state = target.GetComponent<CharacterState>().GetState(States.ComboState) as ComboState;
                if (state == null) return;

                state.MaxStacksCount = _newMaxStackCount;
                
                if(isClient)
                    CmdIncreaseStackOnTargets(target);
            }
        }
    }

    [Command]
    private void CmdIncreaseStackOnTargets(GameObject target)
    {
        var state = target.GetComponent<CharacterState>().GetState(States.ComboState) as ComboState;
        if (state == null) return;
        
        state.MaxStacksCount = _newMaxStackCount; 
    }

    [Command]
    private void CmdDecreaseStacksOnTargets(GameObject target)
    {
        var state = target.GetComponent<CharacterState>().GetState(States.ComboState) as ComboState;
        if (state == null) return;
        
        state.MaxStacksCount = state.InitialStackCount;
        if(state.CurrentStacksCount > state.InitialStackCount)
            state.ReduceStack();
    }

    public void ApplyComboEffect(Transform enemy)
    {
        if (enemy == null) return;

        var targetCharacter = enemy.GetComponent<Character>();
        if (targetCharacter == null) return;

        var stateManager = targetCharacter.CharacterState;
        if (stateManager == null) return;

        var comboState = stateManager.GetState(States.ComboState) as ComboState;
        if (comboState == null || comboState.CurrentStacksCount <= 0)
        {
            if (!_comboTargetsQueue.Contains(targetCharacter))
                _comboTargetsQueue.Add(targetCharacter);
        }

        if (_lastCharacterState != null)
        {
            if (_lastCharacterState != stateManager)
            {
                _lastCharacterState.RemoveState(States.ComboState);
                _lastCharacterState = stateManager;
                _lastCharacterNetId = stateManager.netId;
            }
        }
        else
        {
            _lastCharacterState = stateManager;
            _lastCharacterNetId = stateManager.netId;
        }

        stateManager.AddState(States.ComboState, float.PositiveInfinity, 0f, _hero.gameObject,
            !_isComboStacksIncreased ? nameof(ConsumeCombo_Scorpion) : "ComboIncreaseStacks");
    }

    public void ConsumeCombo_ScorpionPhysicStateClearTalent(bool value)
    {
        if(isConsumeCombo_ScorpionPhysicStateClear == value) return;
        isConsumeCombo_ScorpionPhysicStateClear = value;
    }
    
    public void SetCanCastUnderPhysicalDisable(bool value)
    {
        _canCastUnderPhysicalDisable = value;
    }
    
    private void TryConsumeComboAroundSelf()
    {
        if (!isConsumeCombo_ScorpionPhysicStateClear) return;
        
        CmdDispelPhysState(GetTargetWithCombo(),_healOnDispelBooster.Enabled,_energyOnDispelBooster.Enabled,isDisplePhysState: true);
    }

    private void TryAddComboPoint()
    {
        CmdDispelPhysState(GetTargetWithCombo(),_healOnDispelBooster.Enabled,_energyOnDispelBooster.Enabled,isDisplePhysState: false);
    }

    private List<GameObject> GetTargetWithCombo()
    {
        return Physics.OverlapSphere(transform.position, AreaInfo.Radius, Targeting.Layer)
            .Select(c => c.GetComponent<Character>())
            .Where(c => c != null && c != Hero && c.CharacterState.CheckForState(States.ComboState))
            .Select(c => c.gameObject).ToList();
    }

    [Command]
    private void CmdDispelPhysState(List<GameObject> targetsInRadius,bool healOnDispelEnabled,bool energyOnDispelEnabled,bool isDisplePhysState)
    {
        int totalDispelled = 0;
        
        foreach (var target in targetsInRadius)
        {
            var state = target.GetComponent<CharacterState>().GetState(States.ComboState) as ComboState;
            if (state == null || state.CurrentStacksCount <= 0) continue;
            if (isConsumeCombo_ScorpionPhysicStateClear && isDisplePhysState)
            {
                _hero.CharacterState.DispelStatesStack(StateType.Physical, true, state.CurrentStacksCount, out int dispelled);
                if (state.CurrentStacksCount > 0)
                {
                    if (healOnDispelEnabled)
                        _healOnDispelBooster?.ApplyHealForOneEffect();
                    if (energyOnDispelEnabled)
                        _energyOnDispelBooster?.ApplyEnergyForOneEffect();
                }

                totalDispelled = dispelled;
            }
            if(!isDisplePhysState)
                totalDispelled = state.CurrentStacksCount;
            
            for (int i = 0; i < totalDispelled; i++)
            {
                state.ReduceStack();
                RpcReduceStack(target);
            }
        }
        
        if (totalDispelled > 0 && !isDisplePhysState)
            _comboPoints?.Add(totalDispelled);
    }

    [ClientRpc]
    private void RpcReduceStack(GameObject target)
    {
        target.GetComponent<CharacterState>()?.GetState(States.ComboState)?.ReduceStack();
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (Targeting.GetTempTarget()?.Character == null)
        {
            if (GetMouseButton)
            {
                Vector3 clickPoint = Targeting.GetMousePoint();

                Targeting.FindTempTarget(clickPoint, _clickRadius, canTargetSelf: isConsumeCombo_ScorpionPhysicStateClear);

                var tempCharacter = Targeting.GetTempTarget()?.Character;
                if (tempCharacter != null)
                {
                    bool isSelf = tempCharacter == Hero;
                    bool hasCombo = tempCharacter.CharacterState.CheckForState(States.ComboState);

                    if (isSelf)
                    {
                        if (!isConsumeCombo_ScorpionPhysicStateClear)
                        {
                            Targeting.ClearTempTarget();
                        }
                    }
                    else
                    {
                        if (!hasCombo || !IsEnemyTarget(tempCharacter))
                        {
                            Targeting.ClearTempTarget();
                        }
                    }
                }
            }

            yield return null;
        }

        Targeting.SetTarget(Targeting.GetTempTarget()?.Character);
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Targeting.GetTarget()?.Character);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        var target = Targeting.GetTarget()?.Character;

        if (target == Hero)
        {
            TryConsumeComboAroundSelf();
        }
        else if (target != null && IsEnemyTarget(target))
        {
            TryAddComboPoint();
        }

        yield return null;
    }

    protected override void ClearData() { }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0)
            Targeting.SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
    }
    
    public override bool IsSkillActive
    {
        get => _canCastUnderPhysicalDisable || base.IsSkillActive;
        set => base.IsSkillActive = value;
    }
    
    public override bool Disactive
    {
        get => _disactive;
        set
        {
            if (_canCastUnderPhysicalDisable)
            {
                _disactive = false;
            }
            else if(_disactive != value && !_canCastUnderPhysicalDisable)
            {
                _disactive = value;
            }
        }
    }
    
    public void SetNinjaTalentEnabled(bool value)
    {
        _ninjaTalentEnabled = value;
        UpdateActivationState();
    }

    public void SetFireComboTalentEnabled(bool value)
    {
        _fireComboTalentEnabled = value;
        UpdateActivationState();
    }

    private void UpdateActivationState()
    {
        bool shouldBeActive = _ninjaTalentEnabled || _fireComboTalentEnabled;

        if (shouldBeActive)
        {
            if (!IsSkillActive)
                _hero?.Abilities?.ActivateSkill(this);
        }
        else
        {
            if (IsSkillActive)
                _hero?.Abilities?.DeactivateSkill(this);
        }
    }
}
