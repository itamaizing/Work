using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class UIMenuMainAttributesPanel : MonoBehaviour
{
    [ReadOnly,ShowInInspector]
    public UIMenuMainWindow Owner;
    
    [SerializeField] private UIMenuMainAttributesPanelItem _attributeItem;
    [SerializeField] private RectTransform _itemsParent;
    [SerializeField] private TMProLocalizer _attributesText;

    private AttributeGroup _attributeGroup;
    
    private List<UIMenuMainAttributesPanelItem> _attributes = new ();

    public void Show()
    {
        if(Owner == null) return;
        
        _attributeGroup = Owner.GetHero().Data.Attributes;
        
        ResetPanel();

        foreach (var item in _attributeGroup.AttributeData.Where(o=> o.IsVisible))
        {
            var attribute = Instantiate(_attributeItem, _itemsParent);
            attribute.Owner = this;
            attribute.Fill(item);
            _attributes.Add(attribute);
        }
        
        UpdateAttributesPoints();
    }
    
    private void ResetPanel()
    {
        if (_attributes.Count > 0)
        {
            foreach (var attribute in _attributes)
            {
                attribute.Destroy();
            }
            _attributes.Clear();
        }

        _attributes = new();
    }

    public void ShowHide(bool isShow = true)
    {
        _itemsParent.gameObject.SetActive(_itemsParent.gameObject.activeInHierarchy == false && isShow);
    }

    public void UpdateAttributesPoints()
    {
        SaveManager.Instance.LoadAttributePoints();
        _attributesText.ChangeKey(_attributeGroup.FreeAttributePointsCount);
    }
    
}
