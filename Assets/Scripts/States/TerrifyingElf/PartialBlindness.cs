using System;
using System.Collections.Generic;
using UnityEngine;

public class PartialBlindness : AbstractCharacterState
{
    private float _baseDuration;
    private float _duration;

    #region Const
    private const int MaxStacks = 3;
    private const float BaseMissChancePerStack = 10f;
    private const float EffectivenessDecayPerSecond = 0.02f;
    private const float MaxEffectiveness = 1f;
    private const float MinEffectiveness = 0f;
    #endregion

    private float _currentEffectiveness = 1f;

    private Character _character;
    //private string _talentPartialBlindnessActive;
    private readonly List<StatusEffect> _effects = new() { StatusEffect.Ability };

    public override States State => States.PartialBlindness;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _characterState = character;
        _personWhoMadeBuff = personWhoMadeBuff;

        _baseDuration = durationToExit;
        _duration = _baseDuration;

        //_talentPartialBlindnessActive = skillName;

        MaxStacksCount = MaxStacks;
        CurrentStacksCount = 1;

        _currentEffectiveness = MaxEffectiveness;

        _character = character.GetComponent<Character>();
        _character.Abilities.OnSkillPreparedSuccessfully += HandleSkillPrepared;
    }

    public override void ExitState()
    {
        _character.Abilities.OnSkillPreparedSuccessfully -= HandleSkillPrepared;
        _characterState.RemoveState(this);
        CurrentStacksCount = 0;
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;
        if (_duration <= 0f)
        {
            ExitState();
            return;
        }

        _currentEffectiveness -= EffectivenessDecayPerSecond * Time.deltaTime;
        _currentEffectiveness = Mathf.Clamp(_currentEffectiveness, MinEffectiveness, MaxEffectiveness);
    }

    public override bool Stack(float time)
    {
        _duration = _baseDuration;

        _currentEffectiveness = MaxEffectiveness;

        if (CurrentStacksCount < MaxStacksCount) CurrentStacksCount++;

        return true;
    }

    private void HandleSkillPrepared(Skill skill)
    {
        if (skill == null) return;
        if (skill.AbilityForm != AbilityForm.Physical) return;
        if (skill.Hero != _characterState.Character) return;

        float totalMissChance = CurrentStacksCount * BaseMissChancePerStack * _currentEffectiveness;

        if (UnityEngine.Random.Range(0f, 100f) < totalMissChance)
        {
            skill.CmdCancelActiveSkill();
        }
    }
}
