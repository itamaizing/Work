using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class ConsumeCombo_Scorpion : Skill
{
    private List<Character> _comboTargetsQueue = new List<Character>();

    public int AvailablePoints => _comboTargetsQueue.Sum(target =>
    {
        var state = target.CharacterState.GetState(States.ComboState) as ComboState;
        return state?.CurrentStacksCount ?? 0;
    });

    private CharacterState _lastCharacterState;

    protected override bool IsCanCast => true;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    private bool isConsumeCombo_ScorpionPhysicStateClear;
    private bool _canCastUnderPhysicalDisable = false;
    #region 3% heal per dispelled physical effect
    private bool _healOnDispelActive;
    #endregion

    private float _clickRadius = 0.5f;

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
    
    public void SetHealOnDispelActive(bool value)
    {
        _healOnDispelActive = value;
    }
    
    private void StartDispelHeal()
    {
        if (!_healOnDispelActive) return;
        StartCoroutine(DispelHealCoroutine());
    }
    
    private IEnumerator DispelHealCoroutine()
    {
        const float duration = 9f;
        const float tickInterval = 3f;
        const float healPercentPerEffect = 0.03f;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;

            float healThisTick = _hero.Health.MaxValue * (healPercentPerEffect / 3f);

            Heal heal = new Heal
            {
                Value = healThisTick,
                DamageableSkill = null
            };
            
            ApplyHeal(heal,_hero.Health.gameObject,this,nameof(ConsumeCombo_Scorpion));
        }

        yield return null;
    }

    private void TryConsumeComboAroundSelf()
    {
        if (!isConsumeCombo_ScorpionPhysicStateClear) return;

        List<GameObject> targetsInRadius = Physics.OverlapSphere(transform.position, AreaInfo.Radius, Targeting.Layer)
            .Select(c => c.GetComponent<Character>())
            .Where(c => c != null && c != Hero && c.CharacterState.CheckForState(States.ComboState))
            .Select(c => c.gameObject).ToList();

        CmdDispelPhysState(targetsInRadius);
    }

    [Command]
    private void CmdDispelPhysState(List<GameObject> targetsInRadius)
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
                    StartDispelHeal();
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
}
