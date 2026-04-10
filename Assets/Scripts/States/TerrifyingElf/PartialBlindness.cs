using System;
using System.Collections.Generic;
using UnityEngine;

public class PartialBlindness : StackableState
{
    private float _baseDuration;
    private float _duration;
    
    #region Const
    private const int MaxStacks = 3;
    private const float BaseMissChancePerStack = 10f;
    private const float EffectivenessDecayPerSecond = 2f;
    private const float MinEffectiveness = 0f;
    #endregion

    private float _effectivenessLoss = 0f;

    private Character _character;
    //private string _talentPartialBlindnessActive;
    private readonly List<StatusEffect> _effects = new() { StatusEffect.Ability };

    public override States State => States.PartialBlindness;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;

        _baseDuration = durationToExit;
        _duration = _baseDuration;

        //_talentPartialBlindnessActive = skillName;

        MaxStacksCount = MaxStacks;
        currentStacksCount = 1;

        _character = character.GetComponent<Character>();
        _character.Abilities.OnSkillPreparedSuccessfully += HandleSkillPrepared;
    }

    public override void ExitState()
    {
        _character.Abilities.OnSkillPreparedSuccessfully -= HandleSkillPrepared;
        characterState.RemoveState(this);
        currentStacksCount = 0;
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;
        if (_duration <= 0f)
        {
            ExitState();
            return;
        }

    }

    public override bool Stack(float time)
    {
        _duration = _baseDuration;

        if (CurrentStacksCount < MaxStacksCount) currentStacksCount++;

        return true;
    }

    private void HandleSkillPrepared(Skill skill)
    {
        if (skill == null) return;
        if (skill.Info.AbilityForm != AbilityForm.Physical) return;
        if (skill.Hero != characterState.Character) return;

        _effectivenessLoss = Mathf.Max(MinEffectiveness, (_baseDuration - _duration) * EffectivenessDecayPerSecond); //* CurrentStacksCount)?
        float totalMissChance = CurrentStacksCount * BaseMissChancePerStack - _effectivenessLoss;

        if (UnityEngine.Random.Range(0f, 100f) < 100)
        {
            Debug.Log("сброс способности");
            skill.CmdForceFailCastJobOnce();
        }
    }
}
