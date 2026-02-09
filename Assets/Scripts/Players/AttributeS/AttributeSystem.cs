using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class AttributeSystem : NetworkBehaviour
{
    private CharacterData _data;

    private Dictionary<BasicAttributeName, Attribute> _attributes = new();    
    public Dictionary<BasicAttributeName, Attribute> Attributes => _attributes;

    private ResourceType mainResourceType;
    private Dictionary<ResourceType, ResourceAttribute> _resources = new();
    [SerializeField] public List<ResourceAttribute> TemporaryResourceDisplay = new(); //TMP: Для простоты дебаггинга, потом убрать

    public Dictionary<ResourceType, ResourceAttribute> Resources => _resources;
    public Attribute HPMax => _resources[ResourceType.Health].Attributes[ResourceAttributeName.MaxValue];
    public Attribute HPRegen => _resources[ResourceType.Health].Attributes[ResourceAttributeName.Regen];
    public Attribute ResourceMax => _resources[mainResourceType].Attributes[ResourceAttributeName.MaxValue];
    public Attribute ResourceRegen => _resources[mainResourceType].Attributes[ResourceAttributeName.Regen];
    public Attribute MoveSpeed => _attributes[BasicAttributeName.MoveSpeed];

   // public List<Attribute> Attribute => _attribute;

    //public int Points => _points;

  /*  public void Init2(CharacterData data)
    {
        if (_isInited) return;
        //_data = data;
        _health = data.GetAttribute(AttributeNames.Health);
        _hpRegen = data.GetAttribute(AttributeNames.HpRegen);
        _resourse = data.GetAttribute(AttributeNames.Mana);
        _resourseRegen = data.GetAttribute(AttributeNames.ResourseRegen);
        _moveSpeed = data.GetAttribute(AttributeNames.Speed);
        _physicEvade = data.GetAttribute(AttributeNames.EvasionPhysical);
        _physicResist = data.GetAttribute(AttributeNames.PhysicResist);
        _magicResist = data.GetAttribute(AttributeNames.MagicResist);
        _magicEvade = data.GetAttribute(AttributeNames.MagicEvade);

        _attributes.Add(_health);
        _attributes.Add(_hpRegen);
        _attributes.Add(_resourse);
        _attributes.Add(_resourseRegen);
        _attributes.Add(_moveSpeed);
        _attributes.Add(_physicEvade);
        _attributes.Add(_physicResist);
        _attributes.Add(_magicResist);
        _attributes.Add(_magicEvade);
        Debug.Log("Init");

        foreach (var attribute in _attributes)
        {
            List<AttributeModifiers> modifs = SaveManager.Instance.LoadAttribute(attribute);
            Debug.Log(modifs.Count);
            foreach (var modifier in modifs)
            {
                Debug.Log(modifier.Value + attribute.Name);
                attribute.AddModifier(modifier);
            }
            if(isClient)
                Commands(attribute.Name, modifs);
        }

        _isInited = true;
    }

 [Command]
    private void Commands(string name, List<AttributeModifiers> modifs)
    {
        var attribute = _attributes.FirstOrDefault(n => n.Name == name);
        foreach (var modifier in modifs)
        {
            Debug.Log(modifier.Value + attribute.Name);
            attribute.AddModifier(modifier);
        }
    }*/

    public void Init(CharacterData data)
    {
        _data = data;
        _resources.Add(data.Health.type, new ResourceAttribute(data.Health));
        _resources.Add(data.Resource.type, new ResourceAttribute(data.Resource));
        mainResourceType = data.Resource.type;
        foreach (helperCharData_AttributeInfo info in data.Attributes.AttributeData)
        {
            _attributes.Add(info.type, new Attribute(info.value));
            Debug.Log($"Added {info.type}={info.value}");

        }
        foreach (helperCharData_ResourceInfo info in data.ExtraResources)
        {
            _resources.Add(info.type, new ResourceAttribute(info));
        }
        TemporaryResourceDisplay = _resources.Values.ToList();
    }
}

[Serializable]
public class ResourceAttribute
{
    private Dictionary<ResourceAttributeName, Attribute> _attributes = new ();
    public Dictionary<ResourceAttributeName, Attribute> Attributes => _attributes;
    [SerializeField] public List<Attribute> TemporaryAttributeDisplay = new();

    public ResourceAttribute(helperCharData_ResourceInfo info)
    {
        foreach (var attribute in info.attributes)
        {
            _attributes.Add(attribute.type, new Attribute(attribute.value, attribute.type.ToString()));
            Debug.Log($"Added {attribute.type}={attribute.value}");
        }
        TemporaryAttributeDisplay = _attributes.Values.ToList();
    }
}
