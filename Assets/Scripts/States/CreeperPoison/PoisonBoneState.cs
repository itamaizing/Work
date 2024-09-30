using System.Collections;
using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class PoisonBoneState : AbstractCharacterState
{
    public bool turnOff = false;

    private List<Skill> _skills = new();
    private CreeperStrike _creeperStrike;
    private PoisonBall _poisonBall;

    private int _currentStacks = 0;
    private int _maxStacks = 4;

    private float _timeBetweenAttack;
    private float _startTimeBetweenAttack = 1f;

    private float _duration;
    private float _baseDuration;

    private float _baseDamage = 1f;
    private float _endDamage;

    private Character _player;

    public int CurrentStacks { get => _currentStacks; }

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.AbilitySpeed };

    public override States State => States.PoisonBone;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _characterState = character;
        _player = personWhoMadeBuff;

        _duration = durationToExit;
        _baseDuration = durationToExit;

        if (_player != null)
        {
            _skills = _player.GetComponent<CharacterState>().Character.Abilities.Abilities;
            //Debug.Log("PoisonBone player == " + _player);

            foreach (Skill ability in _skills)
            {
                //Debug.Log("Checking ability: " + ability.name + ", Type: " + ability.GetType());
                if (ability is CreeperStrike creeperStrike)
                {
                    //Debug.Log("if / ability");
                    if (_creeperStrike == null)
                    {
                        _creeperStrike = creeperStrike;
                        //Debug.Log("CreeperStrike == " + _creeperStrike);
                        _creeperStrike.PoisonBoneStacks(_currentStacks);
                    }
                }
                if (ability is PoisonBall poisonBall)
                {
                    if (_poisonBall == null)
                    {
                        _poisonBall = poisonBall;
                        _poisonBall.PoisonBoneStacks(_currentStacks);
                    }

                }
            }
        }

        if (_currentStacks < _maxStacks)
        {
            AddStacks();
        }

    }

    public override void UpdateState()
    {
        _timeBetweenAttack -= Time.deltaTime;
        if (_timeBetweenAttack <= 0)
        {
            DamageDeal();
            _timeBetweenAttack = _startTimeBetweenAttack;
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
        Debug.Log("PoisonBone Stacks = " + _currentStacks);
        if (_currentStacks < _maxStacks)
        {
            AddStacks();
            return true;
        }
        else
        {
            _duration = _baseDuration;
            return false;
        }
    }

    public void AddStacks()
    {
        if (_currentStacks < _maxStacks)
        {
            _currentStacks++;
            //Debug.Log("if / CurrentStackPoisonBone in AddStacks == " + _currentStacks);
            _duration = _baseDuration;
        }
        else
        {
            //Debug.Log("else / CurrentStackPoisonBone in AddStacks == " + _currentStacks);
            _duration = _baseDuration;
        }
    }

    [Server]
    private void DamageDeal()
    {
        Debug.Log("PoisonBone / DamageDeal");
        _endDamage = _currentStacks * _baseDamage;

        Damage damage = new Damage
        {
            Value = _endDamage,
            Type = DamageType.Magical,
            Range = AttackRangeType.Inner
        };

        _characterState.Character.Health.TryTakeDamage(ref damage, _creeperStrike);
    }

    private void ResetValues()
    {
        _currentStacks = 0;
        _baseDuration = 0;
        _duration = 0;
        _endDamage = 0;
        _baseDamage = 1f;
        _timeBetweenAttack = _startTimeBetweenAttack;
    }
}
