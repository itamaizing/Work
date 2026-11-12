using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
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

    public bool IsInteractable { get; private set; } = true;
    public bool CanAddPoint() => IsInteractable && _freeAttributePoints > 0;

    private List<UIMenuMainAttributesPanelItem> _attributes = new ();
    
    public AttributeSystem AttributeSystem => _attributeSystem;
    
    private GameObject _menuAttributeSystemGO;

    private void Awake()
    {
        if(_button != null)
            _button.onClick.AddListener(SwitchPanel);
    }

    public void Show(Character hero, bool isMenu = true, bool isInteractable = true)
    {
        if (_hero == hero && IsInteractable == isInteractable) { RefreshFromSource(); return; }
        if (_hero != null && !isMenu) _hero.AttributeSystem.AttributesReloaded -= RefreshFromSource;

        _hero = hero;
        IsInteractable = isInteractable;
        if (isMenu)
        {
            if (_menuAttributeSystemGO != null)
                Destroy(_menuAttributeSystemGO);

            _menuAttributeSystemGO = new GameObject("MenuAttributeSystem_" + hero.Data.Name);
            _menuAttributeSystemGO.transform.SetParent(transform);
            _menuAttributeSystemGO.AddComponent<NetworkIdentity>();
            _attributeSystem = _menuAttributeSystemGO.AddComponent<AttributeSystem>();
            _attributeSystem.Init(hero.Data);
        }
        else
        {
            MoveTooltipOutsideScroll(this, _descriptionPanel);
            _attributeSystem = _hero.AttributeSystem;
            _attributeSystem.AttributesReloaded += RefreshFromSource;
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
    
    private void RefreshFromSource()
    {
        foreach (var item in _attributes)
            item.SyncModifiers();
        UpdateAttributesPoints();
    }

    [ContextMenu("Run Custom Debug Function")]
    public void SwitchPanel()
    {
        ShowHide(!_itemsParent.gameObject.activeSelf);
    }
    
    private void OnEnable()
    {
        LevelCharacterManager.Instance.OnLevelChanged += OnLevelUp;
    }

    private void OnDisable()
    {
        LevelCharacterManager.Instance.OnLevelChanged -= OnLevelUp;
        if (_attributeSystem != null) _attributeSystem.AttributesReloaded -= RefreshFromSource;
        _hero = null;
        if (_descriptionPanel != null) HideDescription();
        foreach (var attribute in _attributes)
        {
            //attribute.OnValueChange -= UpdateAttributesPoints;
            attribute.OnPointerEntered -= ShowDescription;
            attribute.OnPointerExited -= HideDescription;
        }
    }
    
    private void OnLevelUp(int newLevel)
    {
        UpdateAttributesPoints();
    }

    private void ResetPanel()
    {
        if (_attributes.Count > 0)
        {
            foreach (var attribute in _attributes)
            {
                attribute.gameObject.SetActive(false);
                attribute.Destroy();
            }
            _attributes.Clear();
        }

        _attributes = new();
    }

    public void ShowHide(bool isShow = true)
    {
        _itemsParent.gameObject.SetActive(isShow);
    }

    public void UpdateAttributesPoints()
    {
        foreach (var attribute in _attributes)
            attribute.UpdateValue();

        if (_attributeSystem != null)
        {
            int maxPoints = LevelCharacterManager.Instance.GetCurrentLevel();
            int spent = _attributeSystem.GetSpentPoints();
            _freeAttributePoints = maxPoints - spent;
        }

        _attributesText.ChangeKey(_freeAttributePoints);
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

    internal static void MoveTooltipOutsideScroll(Component panel, Component tooltip)
    {
        var scroll = panel.GetComponentInParent<ScrollRect>(includeInactive: true);
        if (scroll == null || tooltip == null || !tooltip.transform.IsChildOf(scroll.content)) return;

        var rect = (RectTransform)tooltip.transform;
        var window = (RectTransform)scroll.transform.parent;
        bool onRight = window.anchorMin.x > .5f;
        float width = rect.rect.width;
        rect.SetParent(window, false);
        rect.anchorMin = rect.anchorMax = new Vector2(onRight ? 0f : 1f, 1f);
        rect.pivot = new Vector2(onRight ? 1f : 0f, 1f);
        rect.anchoredPosition = new Vector2(onRight ? -8f : 8f, 0f);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        if (!rect.TryGetComponent(out CanvasGroup group))
            group = rect.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
    }

    public void ApplyServerAttributePoints(List<ServerHeroProgressRepository.AttributeEntry> attributes, int freeAttributePoints)
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
