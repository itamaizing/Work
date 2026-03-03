
using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class RegeneratingPoisonState : AbstractCharacterState
{
    /* For SpitPoison Ability */

    private Character _playerWithTalent;

    private int _maxStacks = 5;

    private float _baseHealingValue = 1.0f;
    private float _endHealingValue;

    private float _timeBetweenHeal;
    private float _startTimeBetweenHeal = 1.0f;

    private float _baseDuration;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Healing };

    public override States State => States.RegeneratingPoison;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        MaxStacksCount = _maxStacks;

        _playerWithTalent = personWhoMadeBuff;

        _baseDuration = durationToExit;

        if (currentStacksCount < MaxStacksCount)
        {
            currentStacksCount++;
        }
    }

    public override void UpdateState()
    {
        _timeBetweenHeal -= Time.deltaTime;
        if (_timeBetweenHeal <= 0)
        {
            MakeHeal();
            _timeBetweenHeal = _startTimeBetweenHeal;
        }
    }

    public override void ExitState()
    {
        ResetValues();

        characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (currentStacksCount < MaxStacksCount)
        {
            currentStacksCount++;
            duration = _baseDuration;
            return true;
        }
        else
        {
            duration = _baseDuration;
            return true;
        }
    }

    [Server]
    private void MakeHeal()
    {
        _endHealingValue = currentStacksCount * _baseHealingValue;

        Heal heal = new Heal
        {
            Value = _endHealingValue,
            DamageableSkill = null,
        };

        characterState.Character.Health.Heal(ref heal, null);
        //characterState.Character.DamageTracker.AddHeal(heal);
    }

    private void ResetValues()
    {
        currentStacksCount = 0;
        _endHealingValue = 0;
        _baseDuration = 0;
        duration = 0;
    }
}
