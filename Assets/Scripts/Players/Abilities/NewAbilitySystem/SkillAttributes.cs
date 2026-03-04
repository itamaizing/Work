using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum SkillAttributeName
{
    Damage,
    Radius,
    Area,
    Length,
    Width,
    AttackSpeed,
    CastSpeed,
    Cooldown,
    ResourceCost,
}

public class SkillAttributes
{
    private Dictionary<SkillAttributeName, Attribute> _attributes = new();
    private AttributeSystem _heroAttributes;

    #region Properties
    public Dictionary<SkillAttributeName, Attribute> Attributes => _attributes;
    public float Cooldown
    {
        get
        {
            //if (_heroAttributes == null)
            return _attributes[SkillAttributeName.Cooldown].GetValue();
            //return GetCombined(_attributes[SkillAttributeName.Cooldown],
            //    _heroAttributes.Attributes[BasicAttributeName.C])
            // пока что у персонажа нет атрибута КД
        }
    }
    public float ResourceCost
    {
        get
        {
            if (_heroAttributes == null)
                return _attributes[SkillAttributeName.Cooldown].GetValue();
            return GetCombined(_attributes[SkillAttributeName.ResourceCost],
                _heroAttributes.Attributes[BasicAttributeName.ResourceCost]);
        }
    }
    #endregion Properties

    public SkillAttributes()
    {
        foreach (SkillAttributeName attribute in Enum.GetValues(typeof(SkillAttributeName)))
        {
            _attributes.Add(attribute, new Attribute());
        }
    }

    public void Init(AttributeSystem characterAttributes)
    {
        if (characterAttributes == null)
            Debug.Log("Skill Attributes was null on Init()");
        _heroAttributes = characterAttributes;
    }

    public float GetCombined(Attribute skill, Attribute hero)
    {
        return GetCombined(skill.BaseValue, skill, hero);
    }
    public float GetCombined(float baseValue, Attribute skill, Attribute hero)
    {
        return (baseValue + hero.FlatBonus + skill.FlatBonus) *
            (1 + skill.PercentBonus + skill.PercentBonus) *
            (skill.MultiplierBonus * hero.MultiplierBonus);
    }
}