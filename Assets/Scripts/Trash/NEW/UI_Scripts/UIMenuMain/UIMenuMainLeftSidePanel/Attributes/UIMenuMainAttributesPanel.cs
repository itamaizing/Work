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
    
    private int _freeAttributePoints;

    public bool CanAddPoint() => _freeAttributePoints > 0;

    private bool _isActive = false;

    private List<UIMenuMainAttributesPanelItem> _attributes = new ();
    
    public AttributeSystem AttributeSystem => _attributeSystem;
    
    private GameObject _menuAttributeSystemGO;

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
            if (_menuAttributeSystemGO != null)
                Destroy(_menuAttributeSystemGO);

            _menuAttributeSystemGO = new GameObject("MenuAttributeSystem_" + hero.Data.Name);
            _menuAttributeSystemGO.transform.SetParent(transform);
            _attributeSystem = _menuAttributeSystemGO.AddComponent<AttributeSystem>();
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
            attribute.Fills(item, this);
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
            attribute.UpdateValue();

        if (MPNetworkManager.Instance.IsServer()) //Если айди игрока <=0, то подключения к серверу нет => грузим с PlayerPrefs
        {
            _freeAttributePoints = SaveManager.Instance.LoadAttributePoints();
            _attributesText.ChangeKey(_freeAttributePoints);
        }
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
    
    public void OnAttributePointAdded()
    {
        _freeAttributePoints--;
        _attributesText.ChangeKey(_freeAttributePoints);
    }

    public void OnAttributePointRemoved()
    {
        _freeAttributePoints++;
        _attributesText.ChangeKey(_freeAttributePoints);
    }

    public void ApplyServerAttributePoints(List<TalentNetworkManager.ServerAttributeEntry> attributes, int freeAttributePoints)
    {
        if (_attributeSystem == null) return;

        foreach (var attribute in _attributeSystem.Attributes.Values)
        {
            var entry = attributes?.FirstOrDefault(a => a.name == attribute.Name);
            int targetPoints = entry?.points ?? 0;

            attribute.RemoveBySource("AttributePoint");
            for (int i = 0; i < targetPoints; i++)
                attribute.AddModifier(new AttributeModifier(1, ModifierType.Flat, source: "AttributePoint"));
        }

        foreach (var item in _attributes)
            item.SyncModifiers();

        _freeAttributePoints = freeAttributePoints;
        _attributesText.ChangeKey(_freeAttributePoints);
    }
}
