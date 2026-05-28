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
    CastSpeed,
    Cooldown,
    ResourceCost,
    ChanceModifier,
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
                return _attributes[SkillAttributeName.Cooldown].GetValue();
            return GetCombined(
                _attributes[SkillAttributeName.Cooldown],
                _heroAttributes[CharacterAttributeName.CooldownReduction],
                _attributes[SkillAttributeName.Cooldown].BaseValue);
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
    public float CastSpeed
    {
        get
        {
            if (_heroAttributes == null)
                return _attributes[SkillAttributeName.CastSpeed].GetValue();
            return GetCombined(_attributes[SkillAttributeName.CastSpeed], _heroAttributes[CharacterAttributeName.CastSpeed]);
        }
        set { _attributes[SkillAttributeName.CastSpeed].SetBaseValue(value); }
    }
    public float CastSpeedPhysical
    {
        get
        {
            if (_heroAttributes == null)
                return _attributes[SkillAttributeName.CastSpeed].GetValue();
            var heroB = _heroAttributes[CharacterAttributeName.CastSpeed];
            var heroP = _heroAttributes[CharacterAttributeName.CastSpeedPhysical];
            var skill = _attributes[SkillAttributeName.CastSpeed];

            return (skill.BaseValue + skill.FlatBonus + heroB.FlatBonus + heroP.FlatBonus) *
                (1 + skill.PercentBonus + heroB.FlatBonus + heroP.FlatBonus) *
                (skill.MultiplierBonus + heroB.MultiplierBonus + heroP.MultiplierBonus);
        }
    }
    public float CastSpeedMagical
    {
        get
        {
            if (_heroAttributes == null)
                return _attributes[SkillAttributeName.CastSpeed].GetValue();
            var heroB = _heroAttributes[CharacterAttributeName.CastSpeed];
            var heroM = _heroAttributes[CharacterAttributeName.CastSpeedMagical];
            var skill = _attributes[SkillAttributeName.CastSpeed];

            return (skill.BaseValue + skill.FlatBonus + heroB.FlatBonus + heroM.FlatBonus) *
                (1 + skill.PercentBonus + heroB.FlatBonus + heroM.FlatBonus) *
                (skill.MultiplierBonus + heroB.MultiplierBonus + heroM.MultiplierBonus);
        }
    }
    #endregion Properties

    public SkillAttributes()
    {
        foreach (SkillAttributeName attribute in Enum.GetValues(typeof(SkillAttributeName)))
        {
            Debug.Log(attribute.ToString());
            _attributes.Add(attribute, new Attribute(attribute.ToString()));
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

    public float GetChance(float value)
    {
        if (_heroAttributes == null)
            return _attributes[SkillAttributeName.ChanceModifier].CalculateFor(value);
        return GetCombined(
            _attributes[SkillAttributeName.ChanceModifier],
            _heroAttributes[CharacterAttributeName.ChanceModifier],
            value);
    }
}