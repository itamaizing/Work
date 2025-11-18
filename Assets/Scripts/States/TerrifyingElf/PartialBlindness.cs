using System;
using System.Collections.Generic;
using UnityEngine;

public class PartialBlindness : AbstractCharacterState
{
    private float _baseDuration;
    private float _duration;
    private int _maxStack = 3;
    private float _currentMissChance = 10f;
    private float _currentEffectiveness = 1f;
    private const float _missChanceReductionPerSecond = 0.04f;
    private const float _stackEffectivenessIncrease = 0.2f;
    private const float _cancelChancePerStack = 0.10f;

    private Character _character;

    private string _talentPartialBlindnessActive;
    private List<StatusEffect> _effects = new() { StatusEffect.Ability };

    public override States State => States.PartialBlindness;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        Debug.Log("Entering PartialBlindness");

        _characterState = character;
        _personWhoMadeBuff = personWhoMadeBuff;
        _baseDuration = durationToExit;
        _duration = _baseDuration;
        _currentEffectiveness = 1f;
        _currentMissChance = 1000f; //test
        _talentPartialBlindnessActive = skillName;
        MaxStacksCount = _maxStack;

        _character = character.GetComponent<Character>();

        _character.Abilities.OnSkillPreparedSuccessfully += HandleSkillPrepared;
    }

    public override void ExitState()
    {
        Debug.Log("Exiting PartialBlindness");

        _character.Abilities.OnSkillPreparedSuccessfully -= HandleSkillPrepared;

        _characterState.RemoveState(this);
        CurrentStacksCount = 0;
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;
        if (_duration <= 0)
        {
            ExitState();
            return;
        }

        if (_talentPartialBlindnessActive == "partialBlindnessTalent")
        {
            _currentEffectiveness -= _missChanceReductionPerSecond * Time.deltaTime;
            _currentEffectiveness = Mathf.Max(0f, _currentEffectiveness);
            _currentMissChance = 10f * _currentEffectiveness;
        }
    }

    public override bool Stack(float time)
    {
        if (CurrentStacksCount < MaxStacksCount)
        {
            CurrentStacksCount++;
            _duration = _baseDuration;

            if (_talentPartialBlindnessActive == "partialBlindnessTalent")
            {
                _currentEffectiveness += _stackEffectivenessIncrease;
                _currentEffectiveness = Mathf.Clamp(_currentEffectiveness, 0f, 1f);
            }

            _currentMissChance = 10f * _currentEffectiveness;
            return true;
        }
        else
        {
            _duration = _baseDuration;
            _currentMissChance = 10f * _currentEffectiveness;
            return false;
        }
    }

    private void HandleSkillPrepared(Skill skill)
    {
        if (skill == null) return;
        if (skill.AbilityForm != AbilityForm.Physical) return;
        if (skill.Hero != _characterState.Character) return;

        float totalChance = _cancelChancePerStack * CurrentStacksCount;

        if (_talentPartialBlindnessActive == "partialBlindnessTalent")
        {
            totalChance *= _currentEffectiveness;
        }

        if (UnityEngine.Random.value < 100)
        {
            Debug.Log($"PartialBlindness отменяет скилл: {skill.name}");
            skill.TryCancel();
        }
    }
}
