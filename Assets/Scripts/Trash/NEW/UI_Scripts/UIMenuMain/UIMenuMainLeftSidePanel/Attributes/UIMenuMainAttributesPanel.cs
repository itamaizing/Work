using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIMenuMainAttributesPanel : MonoBehaviour
{
    [SerializeField] private UIMenuMainAttributesPanelItem _attributeItem;
    [SerializeField] private RectTransform _itemsParent;
    [SerializeField] private TMProLocalizer _attributesText;

    [SerializeField] private AttributeDescriptionPanel _descriptionPanel;
    [SerializeField] private Button _button;

    private AttributeSystem _attributeSystem;
    private Character _hero;

    private bool _isActive = false;

    private List<UIMenuMainAttributesPanelItem> _attributes = new ();

    private void Awake()
    {
        if(_button != null)
            _button.onClick.AddListener(SwitchPanel);
    }

    public void Show(Character hero, bool isMenu = true)
    {
        if (_hero == hero) return;
        _hero = hero;
        if (isMenu)
        {
            _attributeSystem = new AttributeSystem();
            //_attributeSystem.Init2(hero.Data);
            _attributeSystem.Init(hero.Data);
        }
        else
        {
            _attributeSystem = _hero.AttributeSystem;
        }
        ResetPanel();

        foreach (var item in _attributeSystem.Attributes.Values)
        //foreach (var item in DB_Attribute.UpgradableAttributes)
        {
            var attribute = Instantiate(_attributeItem, _itemsParent);
            attribute.Fills(item);
            //attribute.Fills(_attributeSystem.Attributes[item]);
            //Debug.Log(item.GetValue());
            //attribute.OnValueChange += UpdateAttributesPoints;
            attribute.OnPointerEntered += ShowDescription;
            attribute.OnPointerExited += HideDescription;

            _attributes.Add(attribute);
        }

        UpdateAttributesPoints();
    }

    [ContextMenu("Run Custom Debug Function")]
    public void SwitchPanel()
    {
        if(_isActive)
        {
            _isActive = false;
            Show(_hero);
        }
        else
        {
            _isActive = true;
            ResetPanel();
        }
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

        _attributesText.ChangeKey(SaveManager.Instance.LoadAttributePoints());
        //_attributesText.ChangeKey(_attributeSystem.Points);
    }
    
    private void ShowDescription(string text)
    {
        if(text != null)
        if(text.Length > 2)
        {
            //Debug.Log(text);
            _descriptionPanel.ShowDesciption(text);
        }
    }

    private void HideDescription()
    {
        _descriptionPanel.HideDescription();
    }
}
