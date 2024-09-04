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
    
    private List<UIMenuMainAttributesPanelItem> _attributes = new ();

    public void Show()
    {
        if(Owner == null) return;
        
        var attributeGroup = Owner.GetHero().Data.Attributes;
        
        ResetPanel();

        foreach (var item in attributeGroup.AttributeData.Where(o=> o.IsVisible))
        {
            var attribute = Instantiate(_attributeItem, _itemsParent);
            attribute.Owner = this;
            attribute.Fill(item);
            _attributes.Add(attribute);
        }
        
        ShowHide(false);
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
    
}
