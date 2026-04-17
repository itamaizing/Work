using UnityEngine;

public class NinjaTalent_9 : Talent
{
    [SerializeField] private IceSword _iceSword;
    [SerializeField] private Character _hero;

    private AttributeModifier _energyMaxModifier;
    private const float BonusEnergyMax = 30f;

    public override void Enter()
    {
        _iceSword.FrozenCrit(true);

        if (_hero == null)
            _hero = GetComponentInParent<Character>();

        if (_hero == null)
            return;

        if (_energyMaxModifier == null)
            _energyMaxModifier = new AttributeModifier(BonusEnergyMax, ModifierType.Flat, this);

        _hero.AttributeSystem.ResourceMax.AddModifier(_energyMaxModifier);

        if (_hero.TryGetResource(ResourceType.Energy, out var resource) && resource is Energy energy)
        {
            energy.Add(BonusEnergyMax);
        }
    }

    public override void Exit()
    {
        _iceSword.FrozenCrit(false);

        if (_hero == null)
            _hero = GetComponentInParent<Character>();

        if (_hero == null || _energyMaxModifier == null)
            return;

        _hero.AttributeSystem.ResourceMax.RemoveModifier(_energyMaxModifier);

        if (_hero.TryGetResource(ResourceType.Energy, out var resource) && resource is Energy energy)
        {
            float maxAfterRemove = _hero.AttributeSystem.ResourceMax.GetValue();

            if (energy.CurrentValue > maxAfterRemove)
            {
                float overflow = energy.CurrentValue - maxAfterRemove;
                energy.TryUse(overflow);
            }
        }
    }
}