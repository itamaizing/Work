using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RegeneratingPoisonState : AbstractCharacterState
{
    public bool turnOff = false;

    private List<Talent> _talents = new();
    private static SurgeTreatment _surgeTreatment;

    private Character _playerWithTalent;

    private int _maxStacks = 5;

    private float _baseHealingValue = 1.0f;
    private float _endHealingValue;
    private float _totalHeal;

    private float _timeBetweenHeal;
    private float _startTimeBetweenHeal = 1.0f;

    private float _duration;
    private float _baseDuration;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Healing };

    public override float TEST_ChangeableValue { get => _baseHealingValue; set => _baseHealingValue = value; }
    public override States State => States.RegeneratingPoison;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        MaxStacksCount = _maxStacks;

        _characterState = character;
        _playerWithTalent = personWhoMadeBuff;

        _duration = durationToExit;
        _baseDuration = durationToExit;

        Debug.Log("_player in EnterRegenPoisonState == " + _playerWithTalent);
        if (_playerWithTalent != null)
        {
            _talents = _playerWithTalent.CharacterState.Character.GetComponent<HeroComponent>().TalentManager.ActiveTalents;
            Debug.Log("HealingPoison player == " + _playerWithTalent);

            foreach (Talent talent in _talents)
            {
                Debug.Log("Checking talents: " + talent.name + ", Type: " + talent.GetType());
                if (talent is SurgeTreatment surgeTreatment)
                {
                    Debug.Log("if / talents");
                    if (_surgeTreatment == null)
                    {
                        _surgeTreatment = surgeTreatment;
                        Debug.Log("SurgeTreatment == " + _surgeTreatment);
                    }
                }
            }
        }

        if (CurrentStacksCount < MaxStacksCount)
        {
            AddStacks();
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

        _duration -= Time.deltaTime;
        if (_duration < 0 || turnOff)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        ResetValues();

        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (CurrentStacksCount < MaxStacksCount)
        {
            AddStacks();
            return true;
        }
        else
        {
            _duration = _baseDuration;
            return true;
        }
    }

    public void AddStacks()
    {
        if (CurrentStacksCount < MaxStacksCount)
        {
            CurrentStacksCount++;
            Debug.Log("if / CurrentStackHealingPoison in AddStacks == " + CurrentStacksCount);
            _duration = _baseDuration;
        }
        else
        {
            Debug.Log("else / CurrentStackHealingPoison in AddStacks == " + CurrentStacksCount);
            _duration = _baseDuration;
        }
    }

    private void MakeHeal()
    {
        Debug.Log("RegenerationPoison / MakeHeal");
        _endHealingValue = CurrentStacksCount * _baseHealingValue;

        Heal heal = new Heal
        {
            Value = _endHealingValue,
            DamageableSkill = null,
        };

        _characterState.Character.Health.Heal(ref heal, null);
        _characterState.Character.DamageTracker.AddHeal(heal);

        if (_surgeTreatment != null && _surgeTreatment.Data.IsOpen)
        {
            _totalHeal += _endHealingValue;
            Debug.Log("TotalHeal RegenerationPoison == " + _totalHeal);
        }
    }

    private void ResetValues()
    {
        CurrentStacksCount = 0;
        _baseDuration = 0;
        _duration = 0;
    }

    public void InstantHeal()
    {
        if (_surgeTreatment != null)
        {
            float totalHeal = _totalHeal;
            Debug.Log("InstantHeal // totalHeal == " + totalHeal); 

            Heal heal = new Heal
            {
                Value = totalHeal,
                DamageableSkill = null,
            };

            _characterState.Character.Health.Heal(ref heal, null);
            _characterState.Character.DamageTracker.AddHeal(heal);

            _totalHeal = 0;
        }
    }
}
