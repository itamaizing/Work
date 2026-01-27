using UnityEngine;
using System.Collections.Generic;

public class AttributeSystem : MonoBehaviour
{
    private CharacterData _data;

    private Dictionary<BasicAttributeName, Attribute> _attributes;    
    public Dictionary<BasicAttributeName, Attribute> Attributes => _attributes;

    private ResourceType mainResourceType;
    private Dictionary<ResourceType, ResourceAttribute> _resources;
    public Dictionary<ResourceType, ResourceAttribute> Resources => _resources;
    public Attribute HPMax => _resources[ResourceType.Health].Attributes[ResourceAttributeName.MaxValue];
    public Attribute HPRegen => _resources[ResourceType.Health].Attributes[ResourceAttributeName.Regen];
    public Attribute ResourceMax => _resources[mainResourceType].Attributes[ResourceAttributeName.MaxValue];
    public Attribute ResourceRegen => _resources[mainResourceType].Attributes[ResourceAttributeName.Regen];
    public Attribute MoveSpeed => _attributes[BasicAttributeName.MoveSpeed];


    public void Init(CharacterData data)
    {
        _data = data;
        _resources.Add(data.Health.type, new ResourceAttribute(data.Health));
        _resources.Add(data.Resource.type, new ResourceAttribute(data.Resource));
        mainResourceType = data.Resource.type;
        foreach (helperCharData_AttributeInfo info in data.Attributes.AttributeData)
        {
            _attributes.Add(info.type, new Attribute(info.value));
        }
        foreach (helperCharData_ResourceInfo info in data.ExtraResources)
        {
            _resources.Add(info.type, new ResourceAttribute(info));
        }
    }
}

public class ResourceAttribute
{
    private Dictionary<ResourceAttributeName, Attribute> _attributes = new ();
    public Dictionary<ResourceAttributeName, Attribute> Attributes => _attributes;

    public ResourceAttribute(helperCharData_ResourceInfo info)
    {
        foreach (var attribute in info.attributes)
        {
            _attributes.Add(attribute.type, new Attribute(attribute.value));
        }
    }
}