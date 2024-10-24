using System.Collections;
using Mirror;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PoisonBoneState : AbstractCharacterState
{
    public bool turnOff = false;

    private List<Skill> _skills = new();
    private CreeperStrike _creeperStrike;
    private PoisonBall _poisonBall;
    private SpitPoison _spitPoison;
    private PoisonSlap _poisonSlap;

    private int _maxStacks = 1000;

    private float _timeBetweenAttack;
    private float _startTimeBetweenAttack = 1f;

    private float _duration;
    private float _baseDuration;

    private float _baseDamage = 1f;
    private float _endDamage;

    private Character _player;

    public int CurrentStacks { get => CurrentStacksCount; set => CurrentStacksCount = value; }
    public float StacksDuration { get => _duration; }

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Poison };
    public override float TEST_ChangeableValue { get; set; }
    public override States State => States.PoisonBone;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        Debug.Log("PoisonBone / EnterState");
        _characterState = character;
        _player = personWhoMadeBuff;

        _duration = durationToExit;
        _baseDuration = durationToExit;
        MaxStacksCount = _maxStacks;

        if (CurrentStacksCount < MaxStacksCount)
        {
            AddStacks();
        }

    }

    private void UpdatePoisonBoneStackAtSkills()    
    {
        if (_player != null)
        {
            _skills = _player.GetComponent<CharacterState>().Character.Abilities.Abilities;
            //Debug.Log("PoisonBone player == " + _player);

            foreach (Skill ability in _skills)
            {
                if (ability is CreeperStrike creeperStrike)
                {
                    if (_creeperStrike == null)
                    {
                        _creeperStrike = creeperStrike;
                        _creeperStrike.PoisonBoneStack = CurrentStacksCount;
                        Debug.Log("PoisonBoneState / _creeperStrike.PoisonBoneStack = " + _creeperStrike.PoisonBoneStack);
                    }
                }
                if (ability is SpitPoison spitPoison)
                {
                    if (_spitPoison == null)
                    {
                        _spitPoison = spitPoison;
                        _spitPoison.PoisonBoneStack = CurrentStacksCount;
                        Debug.Log("PoisonBoneState / _spitPoison.PoisonBoneStack = " + _spitPoison.PoisonBoneStack);

                    }
                }
                if (ability is PoisonBall poisonBall)
                {
                    if (_poisonBall == null)
                    {
                        _poisonBall = poisonBall;
                        _poisonBall.PoisonBoneStack = CurrentStacksCount;
                        Debug.Log("PoisonBoneState / _poisonBall.PoisonBoneStack = " + _poisonBall.PoisonBoneStack);

                    }
                }
                if (ability is PoisonSlap poisonSlap)
                {
                    if (_poisonSlap == null)
                    {
                        _poisonSlap = poisonSlap;
                        _poisonSlap.PoisonBoneStack = CurrentStacksCount;
                        Debug.Log("PoisonBoneState / _poisonSlap.PoisonBoneStack = " + _poisonSlap.PoisonBoneStack);

                    }
                }
            }
        }
    }

    public override void UpdateState()
    {
        if (CurrentStacksCount <= MaxStacksCount)
        {
            _timeBetweenAttack -= Time.deltaTime;
            if (_timeBetweenAttack <= 0)
            {
                DamageDeal();
                _timeBetweenAttack = _startTimeBetweenAttack;
            }
        }

        if (CurrentStacksCount == 0)
        {
            ExitState();
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
            Debug.Log("PoisonBoneState / Stack / if ");

            AddStacks();
            UpdatePoisonBoneStackAtSkills();
            return true;
        }
        else
        {
            Debug.Log("PoisonBoneState / Stack / else ");

            AddStacks();
            UpdatePoisonBoneStackAtSkills();
            return true;
        }
    }

    private void AddStacks()
    {
        if (CurrentStacksCount < MaxStacksCount)
        {
            Debug.Log("PoisonBoneState / AddStacks / if ");

            CurrentStacksCount++;
            //Debug.Log("if / CurrentStackPoisonBone in AddStacks == " + _currentStacks);
            _duration = _baseDuration;
        }
        else
        {
            Debug.Log("PoisonBoneState / AddStacks / else ");

            //Debug.Log("else / CurrentStackPoisonBone in AddStacks == " + _currentStacks);
            _duration = _baseDuration;
        }
    }

    [Server]
    private void DamageDeal()
    {
        _endDamage = CurrentStacksCount * _baseDamage;

        Damage damage = new Damage
        {
            Value = _endDamage,
            Type = DamageType.Magical,
            PhysicAttackType = AttackRangeType.Inner,
        };

        _characterState.Character.Health.TryTakeDamage(ref damage, _creeperStrike);
        _characterState.Character.DamageTracker.AddDamage(damage);
    }

    private void ResetValues()
    {
        CurrentStacksCount = 0;
        _baseDuration = 0;
        _duration = 0;
        _endDamage = 0;
        _baseDamage = 1f;
        _timeBetweenAttack = _startTimeBetweenAttack;
    }
}
