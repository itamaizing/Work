using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIMenuMainAttributesPanelItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image _icon;
    [SerializeField] private TMProLocalizer _attributeValue;

    //private Attribute _currentAttribute;
    private Attribute _currentAttributes;
    private List<AttributeModifier> _modifs = new();

    //public event UnityAction OnValueChange;
    public event UnityAction<string> OnPointerEntered;
    public event UnityAction OnPointerExited;

    public void Fills(Attribute attribute)
    {
        _currentAttributes = attribute;

        foreach (var modif in _currentAttributes.Modifiers)
        {
            _modifs.Add(modif);
        }
        Debug.Log("Attribute " + _attributeValue.name + " " + _modifs.Count);
        // _icon.sprite = _currentAttributes.Icon;
        _attributeValue.ChangeKey(_modifs.Count);
    }

    public void Add()
    {
        var modif = new AttributeModifier(1, ModifierType.MenuFlat);
        _modifs.Add(modif);

        SaveManager.Instance.AddAttributesModif(_currentAttributes, modif);
        _attributeValue.ChangeKey(_modifs.Count);
    }

    public void Reduce()
    {
        if(_modifs.Count <= 0) return;
        
        SaveManager.Instance.RemoveAttributesModif(_currentAttributes, _modifs[0]);
        _modifs.RemoveAt(0);
        _attributeValue.ChangeKey(_modifs.Count);
    }

    public void UpdateValue()
    {
        _attributeValue.ChangeKey(_modifs.Count);
    }
    
    public void Destroy()
    {
        Destroy(gameObject);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log(_currentAttributes.Name);
        if(_currentAttributes != null)
            OnPointerEntered?.Invoke(_currentAttributes.Name);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnPointerExited?.Invoke();   
    }

}
