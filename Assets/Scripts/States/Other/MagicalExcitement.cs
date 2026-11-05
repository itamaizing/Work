using System.Collections.Generic;

public class MagicalExcitement : StateStackingIndependent
{
    private const float RegenBonusPerStack = 0.1f;

    private readonly List<StatusEffect> _effects = new() { StatusEffect.Ability };

    private readonly AttributeModifier _regenModifier = new AttributeModifier(0f, ModifierType.Percent);

    private Attribute _manaRegen;

    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State => States.MagicalExcitement;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    public MagicalExcitement()
    {
        SetMaxStacks(int.MaxValue);
        _regenModifier.Source = this;
    }

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        if (character.isServer)
        {
            _manaRegen = character.Character.TryGetResource(ResourceType.Mana)?.Attr_RegenValue;

            _regenModifier.Value = 0f;
            _manaRegen?.AddModifier(_regenModifier);
        }

        base.Apply(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
    }

    protected override void UpdateDisplayText()
    {
        base.UpdateDisplayText();
        
        if (characterState != null && characterState.isServer)
        {
            _regenModifier.Value = RegenBonusPerStack * CurrentStacksCount;
        }
    }

    public override void ReduceStack()
    {
        if (stacks.Count > 0)
            RemoveStackAt(0);
    }

    protected override void OnExit()
    {
        _manaRegen?.RemoveModifier(_regenModifier);
        _manaRegen = null;

        stacks.Clear();
        _regenModifier.Value = 0f;
        base.UpdateDisplayText();
    }
}