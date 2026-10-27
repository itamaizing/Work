using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClapOfLightUpTalent : Talent
{
    [SerializeField] private Gangdollarff.ClapOfLight _skill;
    public override void Enter()
    {
        _skill.Attributes[SkillAttributeName.Radius].AddModifier(new AttributeModifier(0.5f, ModifierType.Percent, this));
    }

    public override void Exit()
    {
        _skill.Attributes[SkillAttributeName.Radius].RemoveBySource(this);
    }
}
