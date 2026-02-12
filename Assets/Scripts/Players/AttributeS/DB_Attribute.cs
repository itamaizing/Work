using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


public static class DB_Attribute
{
    private static Dictionary<ResourceType, SO_ResourceData> _resourceAttributes = new();
    private static Dictionary<BasicAttributeName, SO_AttributeData> _basicAttributes = new();
    private static Dictionary<BasicAttributeName, SO_AttributeData> _extraAttributes = new();

    public static Dictionary<ResourceType, SO_ResourceData> ResourceAttributes => _resourceAttributes;
    public static Dictionary<BasicAttributeName, SO_AttributeData> BasicAttributes => _basicAttributes;
    public static Dictionary<BasicAttributeName, SO_AttributeData> ExtraAttributes => _extraAttributes;


    public readonly static string AttributeFolder = "Assets/Resources/AttributeSystem"; // По хорошему надо в отдельном конфиг-файле, но пока подобная система одна
    public readonly static string AttributeRelativeFolder = "AttributeSystem"; // По хорошему надо в отдельном конфиг-файле, но пока подобная система одна

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void InitAttributeDatabase()
    {
        _resourceAttributes.Clear();
        _basicAttributes.Clear();
        _extraAttributes.Clear();

        var resources = Resources.LoadAll<SO_ResourceData>($"{AttributeRelativeFolder}/ResourceAttributes");
        foreach ( var resource in resources )
            _resourceAttributes.Add(resource.type, resource);
        
        var basic = Resources.LoadAll<SO_AttributeData>($"{AttributeRelativeFolder}/BasicAttributes");
        foreach ( var attribute in basic )
            _basicAttributes.Add(attribute.type, attribute);
        
        var extra = Resources.LoadAll<SO_AttributeData>($"{AttributeRelativeFolder}/ExtraAttributes");
        foreach ( var attribute in extra )
            _extraAttributes.Add(attribute.type, attribute);
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
        foreach (BasicAttributeName attribute in Enum.GetValues(typeof(BasicAttributeName)))
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
public enum BasicAttributeName
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
    ResourceCost,
}

public enum ResourceAttributeName
{
    MaxValue,
    Regen,
    //ResourceDelay,
}
#endregion
