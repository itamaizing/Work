using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class PoisonCloudState : RefreshingState
{
    private PoisonBall _poisonBall;
    private Character _caster;

    private int _maxStacks = 5;

    private float _baseDamage = 0.005f;

    private float _tickRate = 1f;
    private float _timeToNextTick;

    private float _baseDuration;

    private float _timeToApplyPoisonBone = 3f;
    private float _poisonBoneTimer;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Poison };

    public override States State => States.PoisonCloud;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => _effects;

    public override AbstractCharacterState TryApply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        if (!CanEnterState(character)) return null;

        BaseInit(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);

        if (currentStacksCount == 0)
        {
            MaxStacksCount = _maxStacks;
            EnterState(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
            currentStacksCount = 1;
        }
        else
        {
            Stack(durationToExit);
        }

        return this;
    }

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        health = character.Character.Health;
        this.personWhoMadeBuff = personWhoMadeBuff;

        _caster = personWhoMadeBuff;
        _poisonBall = personWhoMadeBuff != null ? personWhoMadeBuff.GetComponent<PoisonBall>() : null;

        _baseDuration = durationToExit;
        duration = durationToExit;

        _timeToNextTick = _tickRate;
        _poisonBoneTimer = 0f;
    }

    public override void UpdateState()
    {
        _timeToNextTick -= Time.deltaTime;

        if (_timeToNextTick <= 0f)
        {
            DealDamage();
            _timeToNextTick = _tickRate;
        }
    }
    
    public override void ReduceStack()
    {
        currentStacksCount = 0;
        ExitState();
    }

    public override bool Stack(float time)
    {
        duration = _baseDuration;

        if (currentStacksCount < MaxStacksCount)
        {
            currentStacksCount++;
        }

        return true;
    }

    private void DealDamage()
    {
        if (health == null) return;

        float increasedDamage = _baseDamage * currentStacksCount;
        float endDamage = health.MaxValue * increasedDamage;

        Damage damage = new Damage()
        {
            Value = endDamage,
            Type = DamageType.Physical,
        };

        if(!characterState.isServer)
            health.CmdTryTakeDamage(damage, null);

        _poisonBoneTimer += _tickRate;

        if (_poisonBoneTimer >= _timeToApplyPoisonBone)
        {
            if (_poisonBall != null && _poisonBall.IsPoisonCloudAddPoisonBone)
            {
                characterState.AddState(States.PoisonBone, 6, 0, _caster != null ? _caster.gameObject : null, null);
            }

            _poisonBoneTimer = 0f;
        }
    }

    public override void ExitState()
    {
        ResetValues();
        base.ExitState();
    }

    private void ResetValues()
    {
        currentStacksCount = 0;
        _baseDuration = 0;
        duration = 0;
    }
}