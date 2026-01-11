using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class PoisonBoneState : AbstractCharacterState
{
    public bool turnOff = false;

    private List<Skill> _skills = new();
    private CreeperStrike _creeperStrike;
    private PoisonBall _poisonBall;
    private SpitPoison _spitPoison;
    private PoisonSlap _poisonSlap;

    private int _maxStacks = 4;

    private float _timeBetweenAttack;
    private float _startTimeBetweenAttack = 1f;

    private float _duration;
    private float _baseDuration;

    private float _baseDamage = 1f;
    private float _endDamage;

    private Character _player;

    public int CurrentStacks { get => currentStacksCount; set => currentStacksCount = value; }
    public float StacksDuration { get => _duration; }

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Poison };
    public override States State => States.PoisonBone;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        MaxStacksCount = _maxStacks;
        characterState = character;
        _player = personWhoMadeBuff;

        _duration = durationToExit;
        _baseDuration = durationToExit;

        if (currentStacksCount < MaxStacksCount)
        {
            AddStacks();
            UpdatePoisonBoneStackAtSkills();
        }
    }

    private void UpdatePoisonBoneStackAtSkills()    
    {
        if (_player != null)
        {
            _skills = _player.GetComponent<CharacterState>().Character.Abilities.Abilities;

            foreach (Skill ability in _skills)
            {
                if (ability is CreeperStrike creeperStrike)
                {
                    if (_creeperStrike == null)
                    {
                        _creeperStrike = creeperStrike;
                        _creeperStrike.PoisonBoneStack = currentStacksCount;
                    }
                    else
                    {
                        _creeperStrike.PoisonBoneStack = currentStacksCount;
                    }
                }
                if (ability is SpitPoison spitPoison)
                {
                    if (_spitPoison == null)
                    {
                        _spitPoison = spitPoison;
                        _spitPoison.PoisonBoneStack = currentStacksCount;
                    }
                }
                if (ability is PoisonBall poisonBall)
                {
                    if (_poisonBall == null)
                    {
                        _poisonBall = poisonBall;
                        _poisonBall.PoisonBoneStack = currentStacksCount;
                    }
                }
                if (ability is PoisonSlap poisonSlap)
                {
                    if (_poisonSlap == null)
                    {
                        _poisonSlap = poisonSlap;
                        _poisonSlap.PoisonBoneStack = currentStacksCount;
                    }
                }
            }
        }
    }

    public override void UpdateState()
    {
        if (currentStacksCount <= MaxStacksCount)
        {
            _timeBetweenAttack -= Time.deltaTime;
            if (_timeBetweenAttack <= 0)
            {
                DamageDeal();
                _timeBetweenAttack = _startTimeBetweenAttack;
            }
        }

        if (currentStacksCount == 0)
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

        characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (currentStacksCount < MaxStacksCount)
        {
            AddStacks();
            UpdatePoisonBoneStackAtSkills();
            return true;
        }
        else
        {
            _duration = _baseDuration;
            UpdatePoisonBoneStackAtSkills();
            return true;
        }
    }

    private void AddStacks()
    {
        currentStacksCount++;
        _duration = _baseDuration;
    }

    [Server]
    private void DamageDeal()
    {
        _endDamage = currentStacksCount * _baseDamage;

        Damage damage = new Damage
        {
            Value = _endDamage,
            Type = DamageType.Magical,
        };

        characterState.Character.Health.TryTakeDamage(ref damage, _creeperStrike);
        characterState.Character.DamageTracker.AddDamage(damage, null, true);
    }

    private void ResetValues()
    {
        currentStacksCount = 0;
        _baseDuration = 0;
        _duration = 0;
        _endDamage = 0;
        _baseDamage = 1f;
        _timeBetweenAttack = _startTimeBetweenAttack;
        UpdatePoisonBoneStackAtSkills();
    }
}
