using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AttributeSystem : NetworkBehaviour
{
    private CharacterData _data;
    private ResourceType mainResourceType;

    private Dictionary<CharacterAttributeName, Attribute> _attributes = new();
    public Dictionary<CharacterAttributeName, Attribute> Attributes => _attributes;

    private SyncDictionary<CharacterAttributeName, float> _syncAttributes = new();
    public SyncDictionary<CharacterAttributeName, float> SyncAttributes { get => _syncAttributes; }


    private Dictionary<ResourceType, ResourceAttribute> _resources = new();
    public Dictionary<ResourceType, ResourceAttribute> Resources => _resources;
    private SyncDictionary<string, float> _syncResources = new();
    public SyncDictionary<string, float> SyncResources { get => _syncResources; }
    [SerializeField] public List<ResourceAttribute> TemporaryResourceDisplay = new(); //TMP: Для простоты дебаггинга, потом убрать

    public Attribute this[CharacterAttributeName attribute] => _attributes[attribute];
    public Attribute HPMax => _resources[ResourceType.Health].Attributes[ResourceAttributeName.MaxValue];
    public Attribute HPRegen => _resources[ResourceType.Health].Attributes[ResourceAttributeName.Regen];
    public Attribute ResourceMax => _resources[mainResourceType].Attributes[ResourceAttributeName.MaxValue];
    public Attribute ResourceRegen => _resources[mainResourceType].Attributes[ResourceAttributeName.Regen];
    public Attribute MoveSpeed => _attributes[CharacterAttributeName.MoveSpeed];


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
            _attributes.TryAdd(info.type, new Attribute(info.type.ToString(), info.value));
        }
        foreach (helperCharData_ResourceInfo info in data.ExtraResources)
        {
            _resources.TryAdd(info.type, new ResourceAttribute(info));
        }

        foreach (CharacterAttributeName attribute in DB_Attribute.ExtraAttributes)
        {
            float baseValue = 0;
            switch (attribute)
            {
                case CharacterAttributeName.CooldownReduction:
                case CharacterAttributeName.CastSpeed:
                    baseValue = 1;
                    break;
                default:
                    baseValue = 0;
                    break;
            }
            _attributes.TryAdd(attribute, new Attribute(attribute.ToString(), baseValue));
        }
        TemporaryResourceDisplay = _resources.Values.ToList();
        SubscribeToAttributeModify();
    }

    [Server]
    private void HandleAttributeModify(string name, float value)
    {
        Debug.Log($"[Hero Attribute] {_data.Name} {name}: {value}", gameObject);
        if (!Enum.TryParse<CharacterAttributeName>(name, out CharacterAttributeName attr))
            return;
        if (_syncAttributes.ContainsKey(attr))
            _syncAttributes[attr] = value;
        else
            _syncAttributes.Add(attr, value);
    }

    [Server]
    private void HandleResourceModify(string name, float value)
    {
        Debug.Log($"[Hero Resource Attribute] {_data.Name} {name}:{value}", gameObject); 
        if (_syncResources.ContainsKey(name))
            _syncResources[name] = value;
        else
            _syncResources.Add(name, value);
    }

    //Дергаем GetValue() руками, чтобы сразу "отправить" значение на сервер => наполнить SyncDictionary
    [Server]
    private void SubscribeToAttributeModify()
    {
        foreach (Attribute attribute in _attributes.Values)
        {
            attribute.OnAttributeModify += HandleAttributeModify;
            attribute.GetValue();
        }

        foreach (ResourceAttribute resource in _resources.Values)
        {
            resource.OnResourceAttributeModify += HandleResourceModify;
            foreach (Attribute resourceAttribute in resource.Attributes.Values)
                resourceAttribute.GetValue();
        }
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
    public ResourceType type { get; private set; }
    private Dictionary<ResourceAttributeName, Attribute> _attributes = new ();
    public Dictionary<ResourceAttributeName, Attribute> Attributes => _attributes;
    [SerializeField] public List<Attribute> TemporaryAttributeDisplay = new();

    public event Action<string, float> OnResourceAttributeModify;
    public ResourceAttribute(helperCharData_ResourceInfo info)
    {
        type = info.type;
        foreach (var attribute in info.attributes)
        {
            _attributes.TryAdd(attribute.type, new Attribute(attribute.type.ToString(), attribute.value));
            _attributes[attribute.type].OnAttributeModify += SendAttributeModify;
        }
        TemporaryAttributeDisplay = _attributes.Values.ToList();
    }

    // Формат {ResourceType_Attribute}
    private void SendAttributeModify(string name, float value)
    {
        OnResourceAttributeModify?.Invoke($"{type.ToString()}_{name}", value);
    }

    /*
    Можно достать так
    var parts = name.Split('_
    ResourceType resource = Enum.Parse<ResourceType>(parts[0]);
    if (Enum.TryParse(typeof(ResourceAttributeName), parts[1], out var attr) && (ResourceAttributeName)attr == ResourceAttributeName.MaxValue)
    {
        if (gameObject.GetComponent<Character>().Resources.TryGetValue(resource, out var res))
            Debug.Log(res, attr);
    }
    */
}
