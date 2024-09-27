using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealingPoisonPerSecondState : AbstractCharacterState
{
    //private List<Talent> _talents = new();
    //private SurgeTreatment _surgeTreatment;
    public bool turnOff = false;

    private int _currentStack = 0;
    private int _maxStack = 6;

    private float _baseHealingValue;
    private float _totalHealed = 0.0f;
    private float _currentHealingValue;

    private float _timeBetweenHeal;
    private float _startTimeBetweenHeal = 1.0f;

    private float _duration;
    private float _baseDuration;

    private Character _player;
    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.AbilitySpeed };

    public override States State => States.HealingPoisonPerSecond;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        Debug.Log("HealingPoisonPerSecond / EnterState");
        Debug.Log("HealingPoisonPerSecond / EveryState = NewState");

        _characterState = character;
        _player = personWhoMadeBuff;

        _currentHealingValue = 0.0f;

        _duration = durationToExit;
        _baseDuration = durationToExit;
    }

    public override void UpdateState()
    {
        _timeBetweenHeal -= Time.deltaTime;
        if (_timeBetweenHeal <= 0)
        {
            if (_currentStack < _maxStack)
            {
                Debug.Log($"HealingPoisonPerSecond / UpdateState / _baseHealingValue = {_baseHealingValue}");
                MakeHeal();
            }
            else
            {
                return;
            }
            _timeBetweenHeal = _startTimeBetweenHeal;
        }

        _duration -= Time.deltaTime;
        if (_duration < 0 || turnOff)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        Debug.Log("HealingPoisonPerSecond / Stack");
        return false;
    }

    private void MakeHeal()
    {
        _currentHealingValue += 1.0f;

        Debug.Log($"HealingPoisonPerSecond / MakeHeal / _currentHealingValue = {_currentHealingValue}");

        _characterState.Character.Health.Heal(_currentHealingValue);
    }
}
