using System.Collections.Generic;
using UnityEngine;

public class ReversePolarityState : StateBasic
{
    protected override float DescriptionTickInterval => _tickInterval;
    protected override float? DescriptionPercentDamage => _damagePercent * 100f;
    protected override object DescriptionDamage => health != null
        ? CreateDescriptionDamage(health.MaxValue * _damagePercent, DamageType.Magical)
        : new GameDescriptions.DescriptionUnavailable();

    public override States State => States.ReversePolarity;
    public override StateType Type => StateType.Immaterial;
    public override List<StatusEffect> Effects => new List<StatusEffect>();
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    
    private const float _tickInterval  = 1f;
    private const float _damagePercent = 0.01f;
    private float _tickTimer = 0f;

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _tickTimer = 0f;
    }

    public override void UpdateState()
    {
        if (!characterState.isServer) return;

        _tickTimer += Time.deltaTime;

        if (_tickTimer < _tickInterval) return;

        _tickTimer -= _tickInterval;

        float damageValue = health.MaxValue * _damagePercent;

        Damage damage = new Damage
        {
            Value = damageValue,
            Type = DamageType.Magical,
            IsDot = true,
            DamageKey = this.ToString(),
        };

        characterState.Character.TryTakeDamage(ref damage, skill);
    }
}
