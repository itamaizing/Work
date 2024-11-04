using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealingPoisonPerSecondState : AbstractCharacterState
{
    //private List<Talent> _talents = new();
    //private SurgeTreatment _surgeTreatment;
    public bool turnOff = false;

    private int _maxStack = 6;

    private float _baseHealingValue;
    private float _totalHealed = 0.0f;
    private float _currentHealingValue;

    private float _timeBetweenHeal;
    private float _startTimeBetweenHeal = 1.0f;

    private float _duration;
    private float _baseDuration;

    private Character _player;
    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Healing };
    public override float TEST_ChangeableValue { get => _currentHealingValue; set => _currentHealingValue = value; }
    public override States State => States.HealingPoisonPerSecond;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;

    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        Debug.Log("HealingPoisonPerSecond / EnterState");
        Debug.Log("HealingPoisonPerSecond / EveryState = NewState");

        MaxStacksCount = _maxStack;

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
            if (CurrentStacksCount < _maxStack)
            {
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
        //if (CurrentStacksCount < MaxStacksCount)
        //{
        //    Debug.Log("HealingPoisonPerSecond / Stack / if");
        //    CurrentStacksCount++;
        //    _duration = _baseDuration;
        //    return true;
        //}
        //else
        //{
        //    _duration = _baseDuration;
        //    return true;
        //}
        return false;
    }

    private void MakeHeal()
    {
        _currentHealingValue += 1.0f;

        Debug.Log($"HealingPoisonPerSecond / MakeHeal / _currentHealingValue = {_currentHealingValue}");
        Heal heal = new Heal
        {
            Value = _baseHealingValue,
            DamageableSkill = null,
        };

        _characterState.Character.Health.Heal(ref heal, null);
        _characterState.Character.DamageTracker.AddHeal(heal);
    }
}
