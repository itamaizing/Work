using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
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
    
    private UIMenuMainAttributesPanel _owner;

    public void Fills(Attribute attribute, UIMenuMainAttributesPanel owner)
    {
        _owner = owner;
        _currentAttributes = attribute;

        foreach (var modif in _currentAttributes.Modifiers)
        {
            _modifs.Add(modif);
        }
        Sprite attr_icon = DB_Attribute.CharacterAttributes[Enum.Parse<CharacterAttributeName>(attribute.Name)].icon;
        if (attr_icon != null )
            _icon.sprite = attr_icon;
        _attributeValue.ChangeKey(_modifs.Count);
    }

    public void Add()
    {
        if (_owner != null && !_owner.CanAddPoint()) return;

        var modif = new AttributeModifier(1, ModifierType.Flat, source: "AttributePoint");
        _modifs.Add(modif);

        SaveManager.Instance.AddAttributesModif(_currentAttributes, modif);
        _attributeValue.ChangeKey(_modifs.Count);
        _owner?.OnAttributePointAdded();
    }

    public void Reduce()
    {
        if (_modifs.Count <= 0) return;

        SaveManager.Instance.RemoveAttributesModif(_currentAttributes, _modifs[0]);
        _modifs.RemoveAt(0);
        _attributeValue.ChangeKey(_modifs.Count);
        _owner?.OnAttributePointRemoved();
    }

    public void UpdateValue()
    {
        _attributeValue.ChangeKey(_modifs.Count);
    }
    
    public void Destroy()
    {
        Destroy(gameObject);
    }
    
    public void SyncModifiers()
    {
        _modifs.Clear();
        foreach (var modif in _currentAttributes.Modifiers)
            _modifs.Add(modif);
        _attributeValue.ChangeKey(_modifs.Count);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(_currentAttributes != null)
            OnPointerEntered?.Invoke(_currentAttributes.Name);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnPointerExited?.Invoke();   
    }

}
