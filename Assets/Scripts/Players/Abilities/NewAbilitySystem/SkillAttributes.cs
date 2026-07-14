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
                (1 + skill.PercentBonus + heroB.PercentBonus + heroP.PercentBonus) *
                (skill.MultiplierBonus * heroB.MultiplierBonus * heroP.MultiplierBonus);
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
                (1 + skill.PercentBonus + heroB.PercentBonus + heroM.PercentBonus) *
                (skill.MultiplierBonus * heroB.MultiplierBonus * heroM.MultiplierBonus);
        }
    }
    #endregion Properties

    public event Action<string, float> OnAttributeModify;

    public SkillAttributes()
    {
        foreach (SkillAttributeName attribute in Enum.GetValues(typeof(SkillAttributeName)))
        {
            _attributes.Add(attribute, new Attribute(attribute.ToString()));
        }
    }

    public void Init(AttributeSystem characterAttributes)
    {
        if (characterAttributes == null)
            throw new NullReferenceException("Skill Attributes was null on Init()");
        CastSpeed = 1;
        _heroAttributes = characterAttributes;
        SubscribeToAttributeModify();
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

    // Добавить такой же GetDamage. Вероятно вынести их в Skill.cs, чтобы сервер главентсовавал
    // и можно было override'ить
    /// <summary>
    /// Возвращает шанс с учетом модификаторов на скилле и герое
    /// </summary>
    public float GetChance(float value)
    {
        if (_heroAttributes == null)
            return _attributes[SkillAttributeName.ChanceModifier].CalculateFor(value);
        return GetCombined(
            _attributes[SkillAttributeName.ChanceModifier],
            _heroAttributes[CharacterAttributeName.ChanceModifier],
            value);
    }

    private void SubscribeToAttributeModify()
    {
        foreach (Attribute attribute in _attributes.Values)
            attribute.OnAttributeModify += SendAttributeModify;
    }

    private void SendAttributeModify(string name, float value)
    {
        OnAttributeModify?.Invoke(name, value);
    }
}