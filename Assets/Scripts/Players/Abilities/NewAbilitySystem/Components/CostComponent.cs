using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
#if UNITY_EDITOR
public class SkillResourceCost : ISerializationCallbackReceiver
#else
public class SkillResourceCost
#endif
{
#if UNITY_EDITOR
    [HideInInspector]
    public string nameToShow;
#endif
    public ResourceType type;
    public float value;
    public SkillCostType costType;
    /// <summary>
    /// Применяются ли модификаторы затрат
    /// </summary>
    public bool shouldModify = true;
    /// <summary>
    /// Пока заглушка, но как будто компонент может генерировать строку для описания
    /// Вопрос только по локализации
    /// </summary>
    public bool showInDescription = true;

    public void ModifyResourceCost(float multiplier)
    {
        value *= multiplier;
    }

    public void ModifyResourceCost1(float multiplier)
    {
        value /= multiplier;
    }

    public void ResetResourceCost(float baseCost)
    {
        value = baseCost;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Для более читаемого списка в инспекторе
    /// </summary>
    public void OnBeforeSerialize()
    {
        nameToShow = $"{type.ToString()} - {costType.ToString()}";
    }

    public void OnAfterDeserialize() { }
#endif
}


public enum SkillCostType //нужно ли это?
{
    Mandatory, // Обязательная затрата
    Bonus, // Опциональные траты для доп. эффекта (темные стрелы)
    Recast, // Устанавливается после первого нажатия (телепорт в призрака)
    PerSmth, //per meter, per second, etc. | 0 + 4 за метр, 0 + 2 за секунду безмолвия
}
//Бонус ~= PerSmth.
//Бонусный урон к стрелам - x + 1 за 2 урона
//При этом либо во все skillCost пихать лишние макс. траты
//Либо в определять их индивидуально в скилле, тогда зачем это выносить вверх?

[Serializable]
public class CostComponent : BaseSkillComponent
{
    #region InspectorFields
    [SerializeField] protected float _base;
    [SerializeField] protected List<SkillResourceCost> _costs;
    #endregion

    #region Runtime Variables
    private Resource _mainResource;
    private Dictionary<ResourceType, Resource> _resources;
    private Attribute atr_skill, atr_char;
    #endregion

    #region Properties
    public float BaseCost {
        get {
            return CalculateModified(_base, _mainResource.Type);
        }
        set { _base = value; }
    }
    #endregion

    #region Methods
    public override void Init(Skill skill)
    {
        base.Init(skill);
        _mainResource = _character.Resource;
        _resources = _character.Resources;

        atr_skill = _skillAttributes[SkillAttributeName.ResourceCost];
        atr_char = _attributes[CharacterAttributeName.ResourceCost];
    }

    public float CalculateModified(float value, ResourceType type)
    {
        if (type == ResourceType.Rune)
            return value;

        atr_skill.RecalculateMultipliers();
        atr_char.RecalculateMultipliers();
        Debug.Log($"{value} {atr_skill.FlatBonus} {atr_char.FlatBonus}," +
            $"s%{atr_skill.PercentBonus} c%{atr_char.PercentBonus}," +
            $"s*{atr_skill.MultiplierBonus} c*{atr_char.MultiplierBonus}" +
            $"Final:{(value + atr_skill.FlatBonus + atr_char.FlatBonus) * (1 + atr_skill.PercentBonus + atr_char.PercentBonus) * (atr_skill.MultiplierBonus * atr_char.MultiplierBonus)}");
        return _skillAttributes.GetCombined(value, atr_skill, atr_char);
    }

    #region Checks
    public bool EnoughResources(List<SkillResourceCost> costs=null, bool shouldModify=false)
    {
        if (costs == null)
            costs = _costs;
        foreach (SkillResourceCost cost in costs)
        {
            if (_resources.TryGetValue(cost.type, out var resource))
            {
                if (resource.CurrentValue < (shouldModify ? CalculateModified(cost.value, cost.type) : cost.value))
                    return false;
            }
            else
            {
                Debug.LogError("Отсутствует ресурс на персонаже");
                return false;
            }
        }
        return true;
    }

    public bool HasResource(float value, ResourceType type, bool shouldModify=true)
    {
        return false;
    }
    #endregion

    public bool TryPayMandatory() // Проверять дважды? Не круто, суммировать затраты? Тогда PayBase/Extra -> float, что не очень
    {
        if (_mainResource == null || _resources == null)
        {
            Debug.LogError($"Resources are null on {_character.name}");
            return false;
        }
        List<SkillResourceCost> resourcesToPay = new();
        foreach (SkillResourceCost cost in _costs)
            if (cost.costType == SkillCostType.Mandatory)
                resourcesToPay.Add(cost);

        if (TryPayMultiple(resourcesToPay, shouldModify: true))
            return true;

        return false;
    }

    public bool TryPaySingle(float value, ResourceType type, bool shouldModify = true)
    {
        if (_resources.TryGetValue(type, out var resource) == false)
            return false;

        float finalCost = value;
        if (shouldModify)
            finalCost = CalculateModified(value, type);

        if (resource.CurrentValue < finalCost)
            return false;

        resource.CmdUse(finalCost);
        return true;
    }

    public bool TryPayMultiple(List<SkillResourceCost> costs, bool shouldModify = true)
    {
        Dictionary<ResourceType, float> finalCosts = new();
        foreach (var cost in costs)
        {
            if (_resources.TryGetValue(cost.type, out var resource) == false)
                return false;

            float finalCost = cost.value;
            if (shouldModify && cost.shouldModify)
                finalCost = CalculateModified(cost.value, cost.type);

            if (resource.CurrentValue < finalCost)
                return false;

            if (finalCosts.Keys.Contains(cost.type))
                finalCosts[cost.type] += finalCost;
            else
                finalCosts.Add(cost.type, finalCost);
        }

        foreach (var cost in finalCosts)
        {
            Debug.Log($"Paying {cost.Value} {cost.Key}");
            _resources[cost.Key].CmdUse(cost.Value);
        }
        return true;
    }
    #endregion
}
