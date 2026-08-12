using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class PoisonCloudState : RefreshingState
{
    private PoisonBall _poisonBall;
    private Character _caster;

    private int _maxStacks = 5;
    private float _baseDamagePercent = 0.005f;
    private float _auraRadius = 5f;

    private float _tickRate = 1f;
    private float _timeToNextTick;

    private float _baseDuration;

    private float _timeToApplyPoisonBone = 3f;
    private float _poisonBoneTimer;

    private LayerMask _enemyLayer;

    private List<StatusEffect> _effects = new List<StatusEffect>();

    public override States State => States.PoisonCloud;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
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

        _enemyLayer = LayerMask.GetMask("Enemy");

        _caster = character.Character;
        _poisonBall = _caster != null ? _caster.GetComponent<PoisonBall>() : null;

        _baseDuration = durationToExit;
        duration = durationToExit;

        _timeToNextTick = _tickRate;
        _poisonBoneTimer = 0f;
    }

    public override void UpdateState()
    {
        if (!characterState.isServer) return;

        _timeToNextTick -= Time.deltaTime;

        if (_timeToNextTick <= 0f)
        {
            DealAuraDamage();
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

    private void DealAuraDamage()
    {
        if (_caster == null || _caster.IsDead) return;

        Collider[] targets = Physics.OverlapSphere(_caster.transform.position, _auraRadius, _enemyLayer);

        bool dealtDamageThisTick = false;

        foreach (var col in targets)
        {
            if (col == null) continue;

            Character target = col.GetComponent<Character>();
            if (target == null || target == _caster || target.IsDead) continue;
            
            float percentDamage = _baseDamagePercent * currentStacksCount;
            float endDamageValue = target.Health.MaxValue * percentDamage;

            Damage damage = new Damage()
            {
                Value = endDamageValue,
                Type = DamageType.Physical,
            };

            target.Health.TryTakeDamage(ref damage, _poisonBall);
            dealtDamageThisTick = true;

            if (_poisonBoneTimer >= _timeToApplyPoisonBone && _poisonBall != null && _poisonBall.IsPoisonCloudAddPoisonBone)
            {
                target.CharacterState.AddState(States.PoisonBone, 6f, 0, _caster.gameObject, null);
            }
        }

        if (dealtDamageThisTick)
        {
            _poisonBoneTimer += _tickRate;
            if (_poisonBoneTimer >= _timeToApplyPoisonBone)
            {
                _poisonBoneTimer = 0f;
            }
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