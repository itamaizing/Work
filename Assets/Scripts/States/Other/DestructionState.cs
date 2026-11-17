using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

public class DestructionStateStacking : StateStackingRefreshing
{
    private const float DefaultDuration = 12f;
    private const float TickInterval = 3f;
    private const double TickTolerance = 0.001;
    private const float _damagePerTickBase = 6f;

    private double _timeUntilTick;
    private double _remainingDuration;

    private bool _canApplySpiritHealth;

    private readonly List<StatusEffect> _effects = new() { StatusEffect.Destruction };

    public override float RemainingDuration
    {
        get => base.RemainingDuration;
        protected set
        {
            _remainingDuration = Math.Min(value, MaxDuration);
            base.RemainingDuration = value;
        }
    }

    public override States State { get; }
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => _effects;

    public DestructionStateStacking(States stateType)
    {
        State = stateType;
    }
    
    private bool IsStackingMode => State == States.DestructionStacking;

    public override bool TryApply(CharacterState character, float durationToExit, float damageToExit,
        Character sourceCaster, string skillName)
    {
        float duration = durationToExit > 0f ? durationToExit : DefaultDuration;
        MaxDuration = duration;
        return base.TryApply(character, duration, damageToExit, sourceCaster, skillName);
    }

    public override void Apply(CharacterState character, float durationToExit, float damageToExit,
        Character sourceCaster, string skillName)
    {
        SetMaxStacks(IsStackingMode ? 2 : 1);
        CurrentStacksCount = 1;
        _timeUntilTick = TickInterval;
        _canApplySpiritHealth = damageToExit < 0f;
    }

    public override void Reapply(CharacterState character, float durationToExit, float damageToExit,
        Character sourceCaster, string skillName)
    {
        base.Reapply(character, durationToExit, damageToExit, sourceCaster, skillName);
        _canApplySpiritHealth = damageToExit < 0f;
    }

    public override void GlobalUpdate()
    {
        double elapsed = Math.Min(_remainingDuration, Time.deltaTime);
        _remainingDuration -= elapsed;
        base.RemainingDuration = (float)_remainingDuration;
        _timeUntilTick -= elapsed;

        while (_timeUntilTick <= TickTolerance && CurrentStacksCount > 0)
        {
            _timeUntilTick += TickInterval;
            ApplyTick();
        }

        if (CurrentStacksCount > 0 && _remainingDuration <= 0f)
            ReduceStack();
    }

    private void ApplyTick()
    {
        var target = Target;
        if (target == null || target.IsDead || sourceCaster == null) return;

        if (sourceCaster.isOwned)
        {
            var damage = new Damage
            {
                Value = _damagePerTickBase * CurrentStacksCount,
                Type = DamageType.Magical,
                School = Schools.Dark,
                IsDot = true
            };
            sourceCaster.Abilities?.GetSkill<Restoration>()?.CmdApplyDamage(damage, target.gameObject);
        }

        if (NetworkServer.active && _canApplySpiritHealth && Random.value < 0.15f)
            characterState.AddState(States.SpiritHealth, 18f, 0f, target.gameObject, nameof(SpiritHealthStateStacking));
    }

    protected override void OnExit()
    {
        CurrentStacksCount = 0;
        RemainingDuration = 0f;
        _timeUntilTick = TickInterval;
    }
}
