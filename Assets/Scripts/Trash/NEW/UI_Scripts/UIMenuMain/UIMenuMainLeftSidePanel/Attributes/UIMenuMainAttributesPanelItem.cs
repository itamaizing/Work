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
    private Attributes _currentAttributes;
    private List<AttributeModifiers> _modifs = new();

    public event UnityAction OnValueChange;
    public event UnityAction<string> OnPointerEntered;
    public event UnityAction OnPointerExited;


   /* public void Fill(Attribute attribute)
    {
        _currentAttribute = attribute;
        _icon.sprite = _currentAttribute.Icon;
        _attributeValue.Localize(_currentAttribute.Points);
    }*/

    public void Fills(Attributes attribute)
    {
        _currentAttributes = attribute;
        // _icon.sprite = _currentAttributes.Icon;
        _attributeValue.Localize(_modifs.Count);
    }

    public void Add()
    {
        var modif = new AttributeModifiers();
        modif.Value = 1;
        modif.Type = ModifierType.MenuFlat;
        _modifs.Add(modif);

        //SaveManager.Instance.ChangeAttribute(_currentAttribute.Id,1);
        //SaveManager.Instance.LoadAttribute(_currentAttribute.Id);

        SaveManager.Instance.AddAttributesModif(_currentAttributes, modif);
        SaveManager.Instance.LoadAttribute(_currentAttributes);
        
        //_attributeValue.ChangeKey(_currentAttribute.Points);
        
        OnValueChange?.Invoke();
    }

    public void Reduce()
    {
        if(_modifs.Count <= 0) return;
        
        SaveManager.Instance.RemoveAttributesModif(_currentAttributes, _modifs[0]);
        _modifs.RemoveAt(0);

        SaveManager.Instance.LoadAttribute(_currentAttributes);
        
        _attributeValue.ChangeKey((int)_currentAttributes.GetValue());
        
        OnValueChange?.Invoke();
    }

    public void UpdateValue()
    {
        SaveManager.Instance.LoadAttribute(_currentAttributes);
        _attributeValue.ChangeKey((int)_currentAttributes.GetValue());
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
