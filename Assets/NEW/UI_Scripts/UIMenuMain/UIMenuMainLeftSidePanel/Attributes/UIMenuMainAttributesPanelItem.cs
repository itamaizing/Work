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
        var previousValue = _currentAttribute.Points;
        _currentAttribute.Points++;
        
        _attributeValue.ChangeKey(previousValue,_currentAttribute.Points);
        SaveManager.Instance.SaveCurrentHeroData();
    }

    public void Reduce()
    {
        if(_currentAttribute.Points <= 0) return;
        
        var previousValue = _currentAttribute.Points;
        _currentAttribute.Points--;
        
        _attributeValue.ChangeKey(previousValue,_currentAttribute.Points);
        SaveManager.Instance.SaveCurrentHeroData();
    }
    
    public void Destroy()
    {
        Destroy(gameObject);
    }

}
