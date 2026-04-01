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
    public Attribute this[SkillAttributeName attrubute] => _attributes[attrubute];
    public float Cooldown
    {
        get
        {
            if (_heroAttributes == null)
                return GetCombined(
                    _attributes[SkillAttributeName.Cooldown],
                    _heroAttributes[CharacterAttributeName.CooldownReduction],
                    _attributes[SkillAttributeName.Cooldown].BaseValue);
            return _attributes[SkillAttributeName.Cooldown].GetValue();
        }
    }
    public float ResourceCost
    {
        get
        {
            if (_heroAttributes == null)
                return _attributes[SkillAttributeName.ResourceCost].GetValue();
            return GetCombined(_attributes[SkillAttributeName.ResourceCost],
                _heroAttributes[CharacterAttributeName.ResourceCost]);
        }
        set { _attributes[SkillAttributeName.ResourceCost].SetBaseValue(value); }
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

    public float GetCombined(Attribute skill, Attribute hero, float baseValue = float.MinValue)
    {
        if (baseValue == float.MinValue)
            baseValue = skill.BaseValue;
        return (baseValue + hero.FlatBonus + skill.FlatBonus) *
            (1 + skill.PercentBonus + hero.PercentBonus) *
            (skill.MultiplierBonus * hero.MultiplierBonus);
    }

    public float GetCombined(SkillAttributeName skill_atr, CharacterAttributeName hero_atr, float baseValue = float.MinValue)
    {
        return GetCombined(_attributes[skill_atr], _heroAttributes[hero_atr], baseValue);
    }
}