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
        var previousPoints = SaveManager.Instance.GetAttributeValue(_currentAttribute.Id);
        
        SaveManager.Instance.AddAttribute(_currentAttribute.Id,1);
        
        var points = SaveManager.Instance.GetAttributeValue(_currentAttribute.Id);
        
        _attributeValue.ChangeKey(previousPoints,points);
    }

    public void Reduce()
    {
        var previousPoints = SaveManager.Instance.GetAttributeValue(_currentAttribute.Id);
        
        if(previousPoints <= 0) return;
        
        SaveManager.Instance.AddAttribute(_currentAttribute.Id,-1);
        
        var points = SaveManager.Instance.GetAttributeValue(_currentAttribute.Id);
        
        _attributeValue.ChangeKey(previousPoints,points);
    }
    
    public void Destroy()
    {
        Destroy(gameObject);
    }

}
