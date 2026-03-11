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


    private int _points = 0;
    public int Points => _points;

    public void Init(CharacterData data)
    {
        _data = data;
        _resources.TryAdd(data.Health.type, new ResourceAttribute(data.Health));
        _resources.TryAdd(data.Resource.type, new ResourceAttribute(data.Resource));
        mainResourceType = data.Resource.type;
        foreach (helperCharData_AttributeInfo info in data.Attributes.AttributeData)
        {
            _attributes.TryAdd(info.type, new Attribute(info.value));
        }
        foreach (helperCharData_ResourceInfo info in data.ExtraResources)
        {
            _resources.TryAdd(info.type, new ResourceAttribute(info));
        }
        TemporaryResourceDisplay = _resources.Values.ToList();
    }

    //public void InitFromSave()
    //{
    //    foreach (var attribute in _attributes.Values)
    //    {
    //        List<AttributeModifier> modifs =  SaveManager.Instance.LoadAttribute(attribute);
    //        //Debug.Log(modifs.Count + attribute.Name);
    //        foreach (var modifier in modifs)
    //        {
    //            //Debug.Log(modifier.Value + attribute.Name);
    //            attribute.AddModifier(modifier);
    //        }
    //        if(isClient)
    //            Commands(attribute.Name, modifs);
    //    }

    //    _isInited = true;
    //}
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
            _attributes.TryAdd(attribute.type, new Attribute(attribute.value));
        }
        TemporaryAttributeDisplay = _attributes.Values.ToList();
    }
}
