using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


public static class DB_Attribute
{
    private static Dictionary<ResourceType, SO_ResourceData> _resourceAttributes = new();
    private static Dictionary<CharacterAttributeName, SO_AttributeData> _characterAttributes = new();
    //private static Dictionary<CharacterAttributeName, SO_AttributeData> _extraAttributes = new();

    public static Dictionary<ResourceType, SO_ResourceData> ResourceAttributes => _resourceAttributes;
    public static Dictionary<CharacterAttributeName, SO_AttributeData> CharacterAttributes => _characterAttributes;
    //public static Dictionary<CharacterAttributeName, SO_AttributeData> ExtraAttributes => _extraAttributes;



    public static List<CharacterAttributeName> BasicAttributes = new List<CharacterAttributeName>
    {
        CharacterAttributeName.ResistancePhysical,
        CharacterAttributeName.ResistanceMagical,
        CharacterAttributeName.EvasionPhysical,
        CharacterAttributeName.EvasionMagical,
        CharacterAttributeName.MoveSpeed,
        CharacterAttributeName.VisionRadius,
    };
    public static List<CharacterAttributeName> ExtraAttributes = new List<CharacterAttributeName>
    {
        CharacterAttributeName.CastSpeed,
        CharacterAttributeName.CastSpeedPhysical,
        CharacterAttributeName.CastSpeedMagical,
        CharacterAttributeName.CooldownReduction,
        CharacterAttributeName.ResourceCost,
        CharacterAttributeName.OutgoingDamage,
        CharacterAttributeName.DebuffDuration,
    };

    public static List<CharacterAttributeName> UpgradableAttributes = new List<CharacterAttributeName>
    {
        CharacterAttributeName.ResistancePhysical,
        CharacterAttributeName.ResistanceMagical,
        CharacterAttributeName.EvasionPhysical,
        CharacterAttributeName.EvasionMagical,
    };


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
            _characterAttributes.Add(attribute.type, attribute);
        
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
            asset.type = attribute;

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