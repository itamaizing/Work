using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealingPoisonCloudState : AbstractCharacterState
{
    public bool turnOff = false;

    //private List<Skill> _skills = new();
    //private List<Talent> _talents = new();

    private int _maxStacks = 5;
    private float _radiusCloud = 3.5f;

    private float _baseHeal = 0.005f;
    private float _increasedHeal;
    private float _endHeal;

    private float _timeBetweenHeal;
    private float _startTimeBetweenHeal = 1f;

    private static float _duration;
    private static float _baseDuration;

    private Character _player;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Healing };

    public override float TEST_ChangeableValue { get => _baseHeal; set => _baseHeal = value; }

    public override States State => States.HealingPoisonCloud;
    public override StateType Type => StateType.Physical;
    public override BuffDebuff BuffDebuff => BuffDebuff.Buff;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _characterState = character;
        _player = _characterState.Character;

        _duration = durationToExit;
        _baseDuration = durationToExit;

        MaxStacksCount = _maxStacks;

        if (CurrentStacksCount < MaxStacksCount)
        {
            AddStacks();
        }

        //if (_player != null)
        //{
        //    _skills = _player.CharacterState.Character.Abilities.Abilities;
        //    Debug.Log("PoisonCloud player == " + _player);
        //}
    }

    public override void UpdateState()
    {

        _timeBetweenHeal -= Time.deltaTime;
        if (_timeBetweenHeal <= 0)
        {
            SearchingEnemies();
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
            _duration = _baseDuration;
            //Debug.Log("if / CurrentStackPoisonCloud in AddStacks == " + _currentStacks); 
        }
        else
        {
            //Debug.Log("else / CurrentStackPoisonCloud in AddStacks == " + _currentStacks);
            _duration = _baseDuration;
        }
    }

    private void SearchingEnemies()
    {
        Collider2D[] hitAllies = Physics2D.OverlapCircleAll(_characterState.transform.position, _radiusCloud);
        foreach (Collider2D alliesOrPlayer in hitAllies)
        {
            if (alliesOrPlayer.CompareTag("Allies"))
            {
                if (alliesOrPlayer.TryGetComponent<HeroComponent>(out var target))
                {
                    ApplyHealing(target);
                    _timeBetweenHeal = _startTimeBetweenHeal;
                }
            }
        }
    }

    private void ApplyHealing(HeroComponent targetHealth)
    {
        _increasedHeal = _baseHeal * CurrentStacksCount;
        _endHeal = targetHealth.Health.MaxValue * _increasedHeal;
        Heal heal = new Heal
        {
            Value = _endHeal,
            DamageableSkill = null,
        };
        targetHealth.Health.Heal(ref heal, null);
        targetHealth.DamageTracker.AddHeal(heal);
    }

    private void ResetValues()
    {
        CurrentStacksCount = 0;
        _baseDuration = 0;
        _duration = 0;
        _endHeal = 0;
        _increasedHeal = 0;
        _baseHeal = 0.005f;
    }
}
