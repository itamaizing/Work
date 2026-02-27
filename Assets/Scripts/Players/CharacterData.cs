using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "ScriptableObjects/Player", order = 1)]
[Serializable]
public class CharacterData : ScriptableObject
{
    [SerializeField] private string _name;
    [SerializeField] private string _description;
    [SerializeField] private string type;
    [SerializeField] private Sprite _icon;
    [SerializeField] private float _iconSize;
    [SerializeField] private helperCharData_ResourceInfo _health = new helperCharData_ResourceInfo(ResourceType.Health);
    [SerializeField] private helperCharData_ResourceInfo _mainResource = new helperCharData_ResourceInfo(ResourceType.Mana);
    [SerializeField] private List<helperCharData_ResourceInfo> _extraResources;
    [SerializeField] private AttributeGroup _attributes;

    public string Name => _name;
    public string Type => type;
    public string Description => _description;
    public Sprite Icon => _icon;
    public float IconSize => _iconSize;
    public helperCharData_ResourceInfo Health => _health;
    public helperCharData_ResourceInfo Resource => _mainResource;
    public List<helperCharData_ResourceInfo> ExtraResources => _extraResources;

    //public AttributeGroup Attributes => _attributes;

    private void OnValidate()
    {
        Health.OnValidate();
        Resource.OnValidate();
        _attributes.OnValidate();
        foreach (var res in _extraResources)
        {
            if (res != null) res.OnValidate();
        }
    }
}

[Serializable]
public class helperCharData_ResourceInfo
{
    [HideInInspector] public string nameToShow;
    public ResourceType type;
    public List<helperCharData_ResourceValue> attributes = new();
    //public ResourceValue regenDelay;

    public helperCharData_ResourceInfo(ResourceType _type)
    {
        type = _type;
        nameToShow = type.ToString();
        foreach (ResourceAttributeName attr in Enum.GetValues(typeof(ResourceAttributeName)))
        {
            attributes.Add(new helperCharData_ResourceValue(attr));
        }
    }

    public void OnValidate()
    {
        nameToShow = type.ToString();
        if (attributes.Count > 0)
            return;
        foreach (ResourceAttributeName attr in Enum.GetValues(typeof(ResourceAttributeName)))
        {
            attributes.Add(new helperCharData_ResourceValue(attr));
        }
    }

    [Serializable]
    public class helperCharData_ResourceValue
    {
        [HideInInspector] public string nameToShow;
        [HideInInspector] public ResourceAttributeName type;
        public float value;

        public helperCharData_ResourceValue(ResourceAttributeName _type)
        {
            type = _type;
            nameToShow = _type.ToString();
        }
    }
}

[Serializable]
public class helperCharData_AttributeInfo
{
    [HideInInspector] public string nameToShow;
    [HideInInspector] public BasicAttributeName type;
    public float value;
    public helperCharData_AttributeInfo(BasicAttributeName _type, float _value=0)
    {
        type = _type;
        nameToShow = type.ToString();
    }
    
    public void OnValidate()
    {
        nameToShow = type.ToString();
    }
}

[Serializable]
public class Attribute_old
{
    public int Id;
    public string Name;
    public int Points;
    public string Description;

    public float DefaultValue;
    public Sprite Icon;

    public bool IsVisible = false;

    public Attribute_old(int id, string name, int points, string description = null)
    {
        Id = id;
        Name = name;
        Points = points;
        Description = description;
    }
}


[Serializable]
public class AttributeGroup
{
    [SerializeField]
    private List<helperCharData_AttributeInfo> _attributes = new();
    public List<helperCharData_AttributeInfo> AttributeData => _attributes;
    public int FreeAttributePointsCount { get; set; }
    //public int UsedAttributePointsCount => attributesGroup.Sum(o => o.Points);

    public AttributeGroup()
    {
        CreateAttributes();
    }

    public void CreateAttributes()
    {
        foreach (BasicAttributeName attr in Enum.GetValues(typeof(BasicAttributeName)))
        {
            _attributes.Add(new helperCharData_AttributeInfo(attr));
            switch (attr)
            {
                case BasicAttributeName.VisionRadius:
                    _attributes.Last().value = 2;
                    break;
                case BasicAttributeName.MoveSpeed:
                    _attributes.Last().value = 1;
                    break;
            }
        }
    }

    public void SyncronizeAttributes()
    {
        List<helperCharData_AttributeInfo> newAttributes = new();

        foreach (BasicAttributeName enumVal in Enum.GetValues(typeof(BasicAttributeName)))
        {
            var existing = _attributes.FirstOrDefault(a => a.type == enumVal);
            if (existing != null)
            {
                newAttributes.Add(existing);
            }
            else
            {
                newAttributes.Add(new helperCharData_AttributeInfo(enumVal));
            }
        }
        _attributes = newAttributes;
    }

    public void OnValidate()
    {
        if (AttributeData.Count == 0)
        {
            _attributes.Clear();
            CreateAttributes();
            return;
        }
        SyncronizeAttributes();
    }
}
