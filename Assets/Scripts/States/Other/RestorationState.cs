using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class RestorationStateStacking : StateStackingRefreshing
{
    protected override int DescriptionMaxStacks => characterState == null ? (IsStackingMode ? 2 : 1) : MaxStacksCount;
    protected override float DescriptionMaxDuration => MaxDuration >= 0f ? MaxDuration : DefaultDuration;
    protected override float DescriptionTickInterval => TickInterval;
    protected override float? DescriptionHealing => _healPerTickBase * DescriptionStacks;
    protected override float? DescriptionHealingPerStack => _healPerTickBase;

    private const float DefaultDuration = 12f;
    private const float TickInterval = 3f;
    private const double TickTolerance = 0.001;
    private const float _healPerTickBase = 6f;

    private double _timeUntilTick;
    private double _remainingDuration;

    private Character _targetCharacter;

    private readonly List<StatusEffect> _effects = new() { StatusEffect.Restoration };

    public override float RemainingDuration
    {
        get => base.RemainingDuration;
        protected set
        {
            _remainingDuration = Math.Min(value, MaxDuration);
            base.RemainingDuration = value;
        }
    }

    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State { get; }
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    public RestorationStateStacking(States stateType) => State = stateType;

    private bool IsStackingMode => State == States.RestorationStacking;

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
        if (!NetworkServer.active) return;

        sourceCaster?.Abilities?.GetSkill<Restoration>()?.RestorationHealBooster?.Reset();
        _targetCharacter = character.Character;
        _targetCharacter.Health.HealTakedServer += OnTargetHealTaken;
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

        float healValue = _healPerTickBase * CurrentStacksCount + GetSpiritEnergyBonus(target);
        var restoration = sourceCaster.Abilities?.GetSkill<Restoration>();
        if (sourceCaster.isOwned)
        {
            var spark = sourceCaster.Abilities?.GetSkill<SparkOfLight>();
            spark?.OverhealManaBooster?.OnAnyHealTaken(target, healValue, spark);
            restoration?.RestorationManaBooster?.OnRestorationTick(healValue, target);
        }

        if (!NetworkServer.active) return;

        healValue += restoration?.RestorationHealBooster?.BonusHeal ?? 0f;
        var heal = new Heal { Value = healValue };
        health.Heal(ref heal, nameof(RestorationStateStacking));
    }

    private float GetSpiritEnergyBonus(Character character) =>
        (character?.CharacterState?.GetState(States.SpiritEnergy) as SpiritEnergyStateStacking)?.GetHealBonus() ?? 0f;

    protected override void OnExit()
    {
        CurrentStacksCount = 0;
        RemainingDuration = 0f;
        _timeUntilTick = TickInterval;
        if (_targetCharacter != null)
        {
            _targetCharacter.Health.HealTakedServer -= OnTargetHealTaken;
            _targetCharacter = null;
        }
    }

    private void OnTargetHealTaken(float healValue, Skill sourceSkill, string sourceName)
    {
        if (sourceSkill == null || healValue <= 0f) return;
        if (sourceSkill.Hero != sourceCaster) return;

        sourceCaster?.Abilities?.GetSkill<Restoration>()?.RestorationHealBooster?.OnHealReceived(healValue);
    }
}
