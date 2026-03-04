using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ResourceSO", menuName = "ScriptableObjects/Attributes/Resource")]
[Serializable]
public class SO_ResourceData : ScriptableObject
{
    [HideInInspector] public ResourceType type;
    public List<helperSO_ResourceAttribute> attributes = new();

    public void Init(ResourceType _type)
    {
        type = _type;
        foreach (ResourceAttributeName attr in Enum.GetValues(typeof(ResourceAttributeName)))
        {
            attributes.Add(new helperSO_ResourceAttribute(attr));
        }
        OnValidate();
    }

    public void OnValidate()
    {
        foreach (helperSO_ResourceAttribute attr in attributes)
        {
            attr.Update(type);
        }
    }

    [Serializable]
    public class helperSO_ResourceAttribute
    {
        [HideInInspector] public string nameToShow;
        [HideInInspector] public ResourceAttributeName type;
        public Sprite icon;

        [HideInInspector] public string name_locKey;
        [HideInInspector] public string description_locKey;

        public helperSO_ResourceAttribute(ResourceAttributeName _type)
        {
            type = _type;
        }

        public void Update(ResourceType res)
        {
            nameToShow = type.ToString();
            name_locKey = $"{res.ToString()}.{type.ToString()}.name";
            name_locKey = $"{res.ToString()}.{type.ToString()}.description";
        }
    }
}