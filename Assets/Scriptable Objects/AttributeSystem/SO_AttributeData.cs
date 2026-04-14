using System;
using UnityEngine;

[CreateAssetMenu(fileName = "AttributeSO", menuName = "ScriptableObjects/Attributes/Attribute")]
[Serializable]
public class SO_AttributeData : ScriptableObject
{
    [HideInInspector] public CharacterAttributeName type;
    public Sprite icon;

    [HideInInspector] public string name_locKey;
    [HideInInspector] public string description_locKey;

    public void OnValidate()
    {
        //name_locKey = $"attribute.{type.ToString()}.name";
        //description_locKey = $"attribute.{type.ToString()}.description";
        name_locKey = type.ToString();
        description_locKey = "Заглушка описания";
    }
}