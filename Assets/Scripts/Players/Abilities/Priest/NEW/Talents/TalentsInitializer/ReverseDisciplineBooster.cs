using System.Linq;
using Mirror;
using UnityEngine;

public class ReverseDisciplineBooster : SkillTalentHandler
{
    private bool _enabled;

    public ReverseDisciplineBooster(NetworkBehaviour owner) : base(owner)
    {
    }

    public override void Enable(bool value) => _enabled = value;

    public void IsDecreaseManaCost(bool isActive, Character character)
    {
        if (!_enabled) return;
        var priest = Owner.GetComponent<Character>();
        if (priest == null) return;
        foreach (var skill in priest.Abilities.Abilities)
        {
            if (skill.Info.School == Schools.Discipline)
            {
                //skill.Buff.ManaCost.IncreasePercentage(isActive ? 0:1);
                if (isActive)
                    skill.Attributes[SkillAttributeName.ResourceCost].AddModifier(
                        new AttributeModifier(0, ModifierType.Multiplier, source: this));
                else
                    skill.Attributes[SkillAttributeName.ResourceCost].RemoveBySource(this);
            }
        }
    }
}
