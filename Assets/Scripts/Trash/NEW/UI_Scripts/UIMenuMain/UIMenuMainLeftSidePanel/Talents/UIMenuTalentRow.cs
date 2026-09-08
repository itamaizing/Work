using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIMenuTalentRow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private RectTransform _rect;
    [SerializeField] private GameObject _lockedOverlay;

    private List<UIMenuMainTalentsPanelGroupItem> _talents = new();
    private bool _isOpen;
    private string _conditionDescription = "";

    public bool isOpen => _isOpen;
    public RectTransform Rect => _rect;
    public List<UIMenuMainTalentsPanelGroupItem> Talents => _talents;

    public event Action<string> RowPointerEntered;
    public event Action RowPointerExited;

    public void AddTalent(UIMenuMainTalentsPanelGroupItem item) => _talents.Add(item);
    public void SetConditionDescription(string description) => _conditionDescription = description;

    public void SetRowActive(bool active)
    {
        _isOpen = active;
        if (_lockedOverlay != null)
            _lockedOverlay.SetActive(!active);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_isOpen && !string.IsNullOrEmpty(_conditionDescription))
            RowPointerEntered?.Invoke(_conditionDescription);
    }

    public void OnPointerExit(PointerEventData eventData) => RowPointerExited?.Invoke();
}