using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class UIMenuMainAttributesPanelItem : MonoBehaviour
{
    [ReadOnly,ShowInInspector]
    public UIMenuMainAttributesPanel Owner;
    
    [SerializeField] private Image _icon;
    [SerializeField] private TMProLocalizer _attributeValue;

    private Attribute _currentAttribute;

    public void Fill(Attribute attribute)
    {
        _currentAttribute = attribute;
        _icon.sprite = _currentAttribute.Icon;
        _attributeValue.Localize(_currentAttribute.Points);
    }

    public void Add()
    {
        SaveManager.Instance.ChangeAttribute(_currentAttribute.Id,1);
        SaveManager.Instance.LoadAttribute(_currentAttribute.Id);
        
        _attributeValue.ChangeKey(_currentAttribute.Points);
        Owner.UpdateAttributesPoints();
    }

    public void Reduce()
    {
        if(_currentAttribute.Points <= 0 || SaveManager.Instance.LoadAttributePoints() == 0) return;
        
        SaveManager.Instance.ChangeAttribute(_currentAttribute.Id,-1);
        SaveManager.Instance.LoadAttribute(_currentAttribute.Id);
        
        _attributeValue.ChangeKey(_currentAttribute.Points);
        Owner.UpdateAttributesPoints();
    }

    public void UpdateValue()
    {
        SaveManager.Instance.LoadAttribute(_currentAttribute.Id);
        _attributeValue.ChangeKey(_currentAttribute.Points);
    }
    
    public void Destroy()
    {
        Destroy(gameObject);
    }

}
