using System.Collections.Generic;
using UnityEngine;

public class InjectionAdrenalineState : AbstractCharacterState
{
    private float _damageTickTimer;

    private const float DamagePercentPerSecond = 0.05f;
    private const float AttackSpeedMultiplier = 2f;
    private const float MoveSpeedMultiplier = 0.66f;

    private Animator _animator;
    private MoveCreature _moveCreature;

    private float _originalAnimSpeed;
    private float _originalMoveDuration;

    private readonly List<StatusEffect> _effects = new() { StatusEffect.Strengthening };

    public override States State => States.InjectionAdrenaline;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    protected override void OnEnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        health = character.Character.Health;

        duration = durationToExit;

        _animator = character.GetComponent<Animator>();
        _moveCreature = character.GetComponent<MoveCreature>();

        if (_animator != null)
        {
            _originalAnimSpeed = _animator.speed;
            _animator.speed *= AttackSpeedMultiplier;
        }

        if (_moveCreature != null)
        {
            _originalMoveDuration = _moveCreature.MoveDurationPerUnit;
            _moveCreature.MoveDurationPerUnit *= MoveSpeedMultiplier;
        }

        _damageTickTimer = 1f;
    }

    public override void OnUpdateState()
    {
        _damageTickTimer -= Time.deltaTime;

        if (_damageTickTimer <= 0f)
        {
            _damageTickTimer = 1f;

            float damageValue = health.MaxValue * DamagePercentPerSecond;

            Damage damage = new Damage
            {
                Value = damageValue,
                Type = DamageType.None,
                PhysicAttackType = AttackRangeType.MeleeAttack
            };

            health.TryTakeDamage(ref damage, null);
        }
    }

    protected override void OnExitState()
    {
        if (_animator != null)
        {
            _animator.speed = _originalAnimSpeed;
        }

        if (_moveCreature != null)
        {
            _moveCreature.MoveDurationPerUnit = _originalMoveDuration;
        }
    }
}