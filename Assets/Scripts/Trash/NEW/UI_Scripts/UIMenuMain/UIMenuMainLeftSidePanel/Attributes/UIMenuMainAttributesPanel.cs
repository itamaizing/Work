using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIMenuMainAttributesPanel : MonoBehaviour
{
    [SerializeField] private UIMenuMainAttributesPanelItem _attributeItem;
    [SerializeField] private RectTransform _itemsParent;
    [SerializeField] private TMProLocalizer _attributesText;
    [SerializeField] private AttributeDescriptionPanel _descriptionPanel;

    private AttributeGroup _attributeGroup;
    
    private List<UIMenuMainAttributesPanelItem> _attributes = new ();

    public void Show(AttributeGroup attributeGroup)
    {
        //_attributeGroup = attributeGroup;
        
        //ResetPanel();

        //foreach (var item in _attributeGroup.AttributeData.Where(o=> o.IsVisible))
        //{
        //    var attribute = Instantiate(_attributeItem, _itemsParent);
        //    attribute.Fill(item);
        //    attribute.OnValueChange += UpdateAttributesPoints;
        //    attribute.OnPointerEntered += ShowDescription;
        //    attribute.OnPointerExited += HideDescription;

        //    _attributes.Add(attribute);
        //}
        
        //UpdateAttributesPoints();
    }

    private void OnDisable()
    {
        foreach (var attribute in _attributes)
        {
            attribute.OnValueChange -= UpdateAttributesPoints;
            attribute.OnPointerEntered -= ShowDescription;
            attribute.OnPointerExited -= HideDescription;
        }
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
        foreach (var attribute in _attributes)
        {
            attribute.UpdateValue();
        }
        
        SaveManager.Instance.LoadAttributePoints();
        _attributesText.ChangeKey(_attributeGroup.FreeAttributePointsCount);
    }
    
    private void ShowDescription(Attribute_old attribute)
    {
        if(attribute.Description.Length > 2)
        {
            Debug.Log(attribute.Description);
            _descriptionPanel.ShowDesciption(attribute);
        }
    }

    private void HideDescription()
    {
        _descriptionPanel.HideDescription();
    }
}
