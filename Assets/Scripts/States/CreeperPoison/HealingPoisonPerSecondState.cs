using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class HealingPoisonPerSecondState : AbstractCharacterState
{    
    /* For PoisonBall Ability */

    private int _maxStack = 7;

    private float _currentHealingValue;

    private float _timeBetweenHeal;
    private float _startTimeBetweenHeal = 1.0f;

    private float _baseDuration;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Healing };

    public float TotalHealValue { get => _currentHealingValue;}

    public override States State => States.HealingPoisonPerSecond;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        MaxStacksCount = _maxStack;

        characterState = character;

        _currentHealingValue = 0.0f;

        _baseDuration = durationToExit;
        _timeBetweenHeal = _startTimeBetweenHeal;
    }

    public override void UpdateState()
    {
        _timeBetweenHeal -= Time.deltaTime;
        if (_timeBetweenHeal <= 0)
        {
            if (currentStacksCount < _maxStack)
            {
                MakeHeal();
            }

            _timeBetweenHeal = _startTimeBetweenHeal;
        }
    }

    public override void ExitState()
    {
        characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        return false;
    }

    [Server]
    private void MakeHeal()
    {
        _currentHealingValue += 1.0f;

        Heal heal = new Heal
        {
            Value = _currentHealingValue,
            DamageableSkill = null,
        };

        characterState.Character.Health.Heal(ref heal, null);
        //_characterState.Character.DamageTracker.AddHeal(heal, true);
    }
}
