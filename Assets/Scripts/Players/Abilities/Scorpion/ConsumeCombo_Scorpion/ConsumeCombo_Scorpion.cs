using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class ConsumeCombo_Scorpion : Skill
{
    private List<Character> _comboTargetsQueue = new List<Character>();

    private CharacterState _lastCharacterState;
    public CharacterState LastCharacterState
    {
        get => _lastCharacterState;
        set => _lastCharacterState = value;
    }

    protected override bool IsCanCast => true;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    private bool isConsumeCombo_ScorpionPhysicStateClear;
    private bool _canCastUnderPhysicalDisable = false;
    
    #region 3% heal per dispelled physical effect
    private ConsumeComboHealOnDispelBooster _healOnDispelBooster;
    public ConsumeComboHealOnDispelBooster HealOnDispelBooster => _healOnDispelBooster;
    #endregion
    
    #region 5% energy per dispelled physical effect
    private ConsumeComboEnergyOnDispelBooster _energyOnDispelBooster;
    public ConsumeComboEnergyOnDispelBooster EnergyOnDispelBooster => _energyOnDispelBooster;
    #endregion
    
    private float _clickRadius = 0.5f;

    private void OnEnable()
    {
        _healOnDispelBooster = new ConsumeComboHealOnDispelBooster(this);
        _energyOnDispelBooster = new ConsumeComboEnergyOnDispelBooster(this);
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
            }
        }
        else
        {
            _lastCharacterState = stateManager;
        }
        stateManager.AddState(States.ComboState, float.PositiveInfinity, 0f, _hero.gameObject, nameof(ConsumeCombo_Scorpion));
    }

    public void ConsumeCombo_ScorpionPhysicStateClearTalent(bool value)
    {
        isConsumeCombo_ScorpionPhysicStateClear = value;
    }
    
    public void SetCanCastUnderPhysicalDisable(bool value)
    {
        _canCastUnderPhysicalDisable = value;
    }
    
    private void TryConsumeComboAroundSelf()
    {
        if (!isConsumeCombo_ScorpionPhysicStateClear) return;

        List<GameObject> targetsInRadius = Physics.OverlapSphere(transform.position, AreaInfo.Radius, Targeting.Layer)
            .Select(c => c.GetComponent<Character>())
            .Where(c => c != null && c != Hero && c.CharacterState.CheckForState(States.ComboState))
            .Select(c => c.gameObject).ToList();

        CmdDispelPhysState(targetsInRadius,_healOnDispelBooster.Enabled,_energyOnDispelBooster.Enabled);
    }

    [Command]
    private void CmdDispelPhysState(List<GameObject> targetsInRadius,bool healOnDispelEnabled,bool energyOnDispelEnabled)
    {
        foreach (var target in targetsInRadius)
        {
            var state = target.GetComponent<CharacterState>().GetState(States.ComboState) as ComboState;
            if (state == null || state.CurrentStacksCount <= 0) continue;
            if (isConsumeCombo_ScorpionPhysicStateClear)
            {
                _hero.CharacterState.DispelStates(StateType.Physical, true, out int howMuchDispelled, true);
                if (howMuchDispelled > 0)
                {
                    if(healOnDispelEnabled)
                        _healOnDispelBooster?.ApplyHealForOneEffect();
                    if(energyOnDispelEnabled)
                        _energyOnDispelBooster?.ApplyEnergyForOneEffect(); 
                }
            }
            state.ReduceStack();
            RpcReduceStack(target);
        }
    }

    [ClientRpc]
    private void RpcReduceStack(GameObject target)
    {
        target.GetComponent<CharacterState>().GetState(States.ComboState).ReduceStack();
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (Targeting.GetTempTarget()?.Character == null)
        {
            if (GetMouseButton)
            {
                Vector3 clickPoint = Targeting.GetMousePoint();

                Targeting.FindTempTarget(clickPoint, _clickRadius, canTargetSelf: true);

                if (Targeting.GetTempTarget()?.Character)
                {
                    if (Targeting.GetTempTarget()?.Character != null && Targeting.GetTempTarget()?.Character != Hero)
                    {
                        Targeting.ClearTempTarget();
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
        TryConsumeComboAroundSelf();

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
}
