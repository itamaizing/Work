using System.Collections.Generic;
using Mirror;

public class MetabolismReptileState : StateBasic
{
    private const float RegenPercent = 1f;
    private const float CooldownReduction = 0.5f;
    private const float CastSpeedFactor = 2f;

    private static readonly List<StatusEffect> _effects = new()
    {
        StatusEffect.Strengthening, StatusEffect.AbilitySpeed
    };

    private bool _castBuffApplied;

    public override States State => States.MetabolismReptile;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public override void Apply(CharacterState character, float durationToExit, float damageToExit,
        Character sourceCaster, string skillName)
    {
        Revert();

        var hero = character.Character;
        
        if (NetworkServer.active)
        {
            hero.Health.Attr_RegenValue.AddModifier(
                new AttributeModifier(RegenPercent, ModifierType.Percent, source: this));
            hero.Resource.Attr_RegenValue.AddModifier(
                new AttributeModifier(RegenPercent, ModifierType.Percent, source: this));
        }
        
        if (hero.isOwned)
        {
            hero.AttributeSystem[CharacterAttributeName.CooldownReduction].AddModifier(
                new AttributeModifier(CooldownReduction, ModifierType.Flat, source: this));

            ApplyCastSpeed(true);
        }

        hero.GetComponent<CreeperPoisonAura>()?.ActiveWitheringPoisonMetabolism(true);
    }

    public override void Reapply(CharacterState character, float durationToExit, float damageToExit,
        Character sourceCaster, string skillName) { }

    protected override void OnExit() => Revert();

    private void Revert()
    {
        var hero = characterState != null ? characterState.Character : null;
        if (hero == null) return;

        if (NetworkServer.active)
        {
            hero.Health.Attr_RegenValue.RemoveBySource(this, all: true);
            hero.Resource.Attr_RegenValue.RemoveBySource(this, all: true);
        }

        if (hero.isOwned)
        {
            hero.AttributeSystem[CharacterAttributeName.CooldownReduction].RemoveBySource(this, all: true);
            ApplyCastSpeed(false);
        }

        hero.GetComponent<CreeperPoisonAura>()?.ActiveWitheringPoisonMetabolism(false);
    }

    private void ApplyCastSpeed(bool apply)
    {
        if (abilities == null) return;
        if (apply == _castBuffApplied) return;

        var ball = abilities.GetSkill<PoisonBall>();
        var spit = abilities.GetSkill<SpitPoison>();

        if (apply)
        {
            ball?.Buff.CastSpeed.ReductionPercentage(CastSpeedFactor);
            spit?.Buff.CastSpeed.ReductionPercentage(CastSpeedFactor);
        }
        else
        {
            ball?.Buff.CastSpeed.IncreasePercentage(CastSpeedFactor);
            spit?.Buff.CastSpeed.IncreasePercentage(CastSpeedFactor);
        }
        _castBuffApplied = apply;
    }
}