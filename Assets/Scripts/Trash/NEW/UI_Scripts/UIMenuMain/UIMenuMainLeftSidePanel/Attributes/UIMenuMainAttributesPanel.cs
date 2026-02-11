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

    private AttributeSystem _attributeSystem;
    
    private List<UIMenuMainAttributesPanelItem> _attributes = new ();

    public void Show(Character hero)
    {
        _attributeSystem = new AttributeSystem();
        _attributeSystem.Init2(hero.Data);

        ResetPanel();

        foreach (var item in _attributeSystem.Attributes)
        {
            var attribute = Instantiate(_attributeItem, _itemsParent);
            attribute.Fills(item);
            Debug.Log(item.GetValue());
            //attribute.OnValueChange += UpdateAttributesPoints;
            attribute.OnPointerEntered += ShowDescription;
            attribute.OnPointerExited += HideDescription;

            _attributes.Add(attribute);
        }

        UpdateAttributesPoints();
    }

    private void OnDisable()
    {
        foreach (var attribute in _attributes)
        {
            //attribute.OnValueChange -= UpdateAttributesPoints;
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
        //_attributesText.ChangeKey(_attributeSystem.Points);
    }
    
    private void ShowDescription(string text)
    {
        if(text.Length > 2)
        {
            Debug.Log(text);
            _descriptionPanel.ShowDesciption(text);
        }
    }

    private void HideDescription()
    {
        _descriptionPanel.HideDescription();
    }
}
