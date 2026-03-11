using UnityEngine;

public interface IAttribute 
{
    public void AddModifier(AttributeModifier modif);
    public void RemoveModifier(AttributeModifier modif);
}
