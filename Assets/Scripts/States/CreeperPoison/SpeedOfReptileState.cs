using System.Collections.Generic;
using Mirror;

public class SpeedOfReptileState : StateBasic
{
    private static readonly List<StatusEffect> _effects = new()
    {
        StatusEffect.Strengthening, StatusEffect.AbilitySpeed, StatusEffect.Evade
    };

    private AttributeModifier _moveModifier;

    public override States State => States.SpeedOfReptile;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character sourceCaster, string skillName)
    {
        var hero = character.Character;
        var speed = abilities.GetSkill<SpeedOfReptile>();
        
        if (NetworkServer.active || hero.isOwned)
        {
            hero.AttributeSystem[CharacterAttributeName.CastSpeedPhysical].AddModifier(new AttributeModifier(speed != null ? speed.CastSpeedMultiplier : 2f, ModifierType.Multiplier, this));

            _moveModifier = new AttributeModifier(speed != null ? speed.MoveSpeedMultiplier : 2f, ModifierType.Multiplier, this);
            hero.Move.AddModifier(_moveModifier);
        }

        if (!NetworkServer.active) return;

        float evasionMultiplier = speed != null ? speed.EvasionMultiplier : 2f;
        hero.AttributeSystem[CharacterAttributeName.EvasionPhysicalMelee].AddModifier(new AttributeModifier(evasionMultiplier, ModifierType.Multiplier, this));
        hero.AttributeSystem[CharacterAttributeName.EvasionPhysicalRange].AddModifier(new AttributeModifier(evasionMultiplier, ModifierType.Multiplier, this));
        hero.AttributeSystem[CharacterAttributeName.EvasionMagical].AddModifier(new AttributeModifier(evasionMultiplier, ModifierType.Multiplier, this));
    }

    public override void Reapply(CharacterState character, float durationToExit, float damageToExit, Character sourceCaster, string skillName) { }

    protected override void OnExit()
    {
        var hero = characterState.Character;
        if (NetworkServer.active || hero.isOwned)
        {
            hero.AttributeSystem[CharacterAttributeName.CastSpeedPhysical].RemoveBySource(this);

            if (_moveModifier != null)
            {
                hero.Move.RemoveModifier(_moveModifier);
                _moveModifier = null;
            }
        }

        if (!NetworkServer.active) return;

        hero.AttributeSystem[CharacterAttributeName.EvasionPhysicalMelee].RemoveBySource(this);
        hero.AttributeSystem[CharacterAttributeName.EvasionPhysicalRange].RemoveBySource(this);
        hero.AttributeSystem[CharacterAttributeName.EvasionMagical].RemoveBySource(this);
    }
}
