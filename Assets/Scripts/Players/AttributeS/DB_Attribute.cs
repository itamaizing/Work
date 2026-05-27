using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;


public static class DB_Attribute
{
    private static Dictionary<ResourceType, SO_ResourceData> _resourceAttributes = new();
    private static Dictionary<CharacterAttributeName, SO_AttributeData> _characterAttributes = new();
    public static Dictionary<SkillAttributeName, SO_AttributeData> _skillAttributes = new();
    
    public static Dictionary<ResourceType, SO_ResourceData> ResourceAttributes => _resourceAttributes;
    public static Dictionary<CharacterAttributeName, SO_AttributeData> CharacterAttributes => _characterAttributes;
    public static Dictionary<SkillAttributeName, SO_AttributeData> SkillAttributes => _skillAttributes;

    #region Static Lists
    public static List<CharacterAttributeName> BasicAttributes = new List<CharacterAttributeName>
    {
        CharacterAttributeName.ResistancePhysical,
        CharacterAttributeName.ResistanceMagical,
        CharacterAttributeName.EvasionPhysical,
        CharacterAttributeName.EvasionMagical,
        CharacterAttributeName.MoveSpeed,
        CharacterAttributeName.VisionRadius,
    };

    public static readonly List<CharacterAttributeName> ExtraAttributes =
        Enum.GetValues(typeof(CharacterAttributeName))
            .Cast<CharacterAttributeName>()
            .Except(BasicAttributes)
            .ToList();

    public static List<CharacterAttributeName> UpgradableAttributes = new List<CharacterAttributeName>
    {
        CharacterAttributeName.ResistancePhysical,
        CharacterAttributeName.ResistanceMagical,
        CharacterAttributeName.EvasionPhysical,
        CharacterAttributeName.EvasionMagical,
    };
    #endregion

    public readonly static string AttributeFolder = "Assets/Resources/AttributeSystem"; // По хорошему надо в отдельном конфиг-файле, но пока подобная система одна
    public readonly static string AttributeRelativeFolder = "AttributeSystem"; // По хорошему надо в отдельном конфиг-файле, но пока подобная система одна

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void InitAttributeDatabase()
    {
        _resourceAttributes.Clear();
        _characterAttributes.Clear();
        //_extraAttributes.Clear();

        var resources = Resources.LoadAll<SO_ResourceData>($"{AttributeRelativeFolder}/ResourceAttributes");
        foreach ( var resource in resources )
            _resourceAttributes.Add(resource.type, resource);
        
        var basic = Resources.LoadAll<SO_AttributeData>($"{AttributeRelativeFolder}/BasicAttributes");
        foreach ( var attribute in basic )
            _characterAttributes.Add(Enum.Parse<CharacterAttributeName>(attribute.type), attribute);

        var skill = Resources.LoadAll<SO_AttributeData>($"{AttributeRelativeFolder}/SkillAttributes");
        foreach (var attribute in skill )
            _skillAttributes.Add(Enum.Parse<SkillAttributeName>(attribute.type), attribute);

        var extra = Resources.LoadAll<SO_AttributeData>($"{AttributeRelativeFolder}/ExtraAttributes");
        //foreach ( var attribute in extra )
        //    _extraAttributes.Add(attribute.type, attribute);
        //Debug.Log(_resourceAttributes.Count);
    }

    public static Type GetResourceClass(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Health: return typeof(Health);
            case ResourceType.Mana: return typeof(Mana);
            case ResourceType.Energy: return typeof(Energy);
            case ResourceType.Rune: return typeof(RuneComponent);
            case ResourceType.Psionic: return typeof(BasePsionicEnergy);
            case ResourceType.CooldownEnergy: return typeof(CooldownEnergy);
            default: return null;
        }
    }

#if UNITY_EDITOR
    [MenuItem("Tools/Attributes/GenerateMissing()"), MenuItem("Assets/Methods/Attributes/GenerateMissing()", false, 100)]
    public static void Generate()
    {
        string path = $"{AttributeFolder}/ResourceAttributes";
        foreach (ResourceType resource in Enum.GetValues(typeof(ResourceType)))
        {
            if (AssetDatabase.LoadAssetAtPath($"{path}/{resource}.asset", typeof(SO_ResourceData)) != null) continue;

            var asset = ScriptableObject.CreateInstance<SO_ResourceData>();
            asset.Init(resource);
            AssetDatabase.CreateAsset(
                asset,
                $"{path}/{resource}.asset"
            );
        }
        path = $"{AttributeFolder}/BasicAttributes";
        foreach (CharacterAttributeName attribute in Enum.GetValues(typeof(CharacterAttributeName)))
        {
            if (AssetDatabase.LoadAssetAtPath($"{path}/{attribute}.asset", typeof(SO_AttributeData)) != null) continue;

            var asset = ScriptableObject.CreateInstance<SO_AttributeData>();
            asset.type = attribute.ToString();

            AssetDatabase.CreateAsset(
                asset,
                $"{path}/{attribute}.asset"
            );
        }
        path = $"{AttributeFolder}/SkillAttributes";
        foreach (CharacterAttributeName attribute in Enum.GetValues(typeof(SkillAttributeName)))
        {
            if (AssetDatabase.LoadAssetAtPath($"{path}/{attribute}.asset", typeof(SO_AttributeData)) != null) continue;

            var asset = ScriptableObject.CreateInstance<SO_AttributeData>();
            asset.type = attribute.ToString();

            AssetDatabase.CreateAsset(
                asset,
                $"{path}/{attribute}.asset"
            );
        }

        AssetDatabase.SaveAssets();
    }
#endif
}

#region enums
public enum CharacterAttributeName
{
    ResistancePhysical,
    ResistanceMagical,
    EvasionPhysical,
    EvasionMagical,
    MoveSpeed,
    VisionRadius,
    CastSpeed,
    CastSpeedPhysical,
    CastSpeedMagical,
    CooldownReduction,
    ResourceCost,
    OutgoingDamage,
    DebuffDuration,
    ChanceModifier,
}

public enum ResourceAttributeName
{
    MaxValue,
    Regen,
    RegenDelay,
    RegenPeriod,
}
#endregion
//атрибуты
/*
 * Крит шанс, урон -> не атрибут персонажа, может быть атрибутом способности
 * Весь входящий урон -> трогаем отдельно резисты
 * Исходящий урон -> атрибут
 * Входящий контроль -> атрибут. Время, но не эффективность
 * Исходящий контроль -> не делаем
 * Затрачиваемые ресурсы -> выносим, оставляем в способности
 * Общая дальность заклинаний -> не выносим
 * Кулдауны -> выносим в атрибут
 */