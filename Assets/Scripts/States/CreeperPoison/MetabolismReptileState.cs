using System.Collections.Generic;
using Mirror;

public class MetabolismReptileState : StateBasic
{
    private const float RegenMultiplier = 2f;
    private const float CooldownReduction = 0.5f;
    private const float CastSpeedFactor = 2f;

    private static readonly List<StatusEffect> _effects = new()
    {
        StatusEffect.Strengthening, StatusEffect.AbilitySpeed
    };

    public override States State => States.MetabolismReptile;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character sourceCaster, string skillName)
    {
        Revert();

        var hero = character.Character;
        
        if (NetworkServer.active)
        {
            hero.Health.Attr_RegenValue.AddModifier(new AttributeModifier(RegenMultiplier, ModifierType.Multiplier, source: this));
        }
        
        if (NetworkServer.active || hero.isOwned)
        {
            hero.AttributeSystem[CharacterAttributeName.CooldownReduction].AddModifier(new AttributeModifier(CooldownReduction, ModifierType.Multiplier, source: this));

            hero.AttributeSystem[CharacterAttributeName.CastSpeed].AddModifier(new AttributeModifier(CastSpeedFactor, ModifierType.Multiplier, source: this));
        }

        hero.GetComponent<CreeperPoisonAura>()?.ActiveWitheringPoisonMetabolism(true);
    }

    public override void Reapply(CharacterState character, float durationToExit, float damageToExit, Character sourceCaster, string skillName) { }

    protected override void OnExit() => Revert();

    private void Revert()
    {
        var hero = characterState != null ? characterState.Character : null;
        if (hero == null) return;

        if (NetworkServer.active)
        {
            hero.Health.Attr_RegenValue.RemoveBySource(this, all: true);
        }

        if (NetworkServer.active || hero.isOwned)
        {
            hero.AttributeSystem[CharacterAttributeName.CooldownReduction].RemoveBySource(this, all: true);
            hero.AttributeSystem[CharacterAttributeName.CastSpeed].RemoveBySource(this, all: true);
        }

        hero.GetComponent<CreeperPoisonAura>()?.ActiveWitheringPoisonMetabolism(false);
    }

}
