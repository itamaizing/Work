using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMenuMainTalentsPanel : MonoBehaviour
{
    [SerializeField] private UIMenuMainAttributesPanel _attributesPanel;
    [SerializeField] private UIMenuMainTalentsPanelGroup _talentsPanelGroup;
    [SerializeField] private RectTransform _itemsParent;
    [SerializeField] private TalentInfoPanel _talentInfoPanel;
    [SerializeField] private TMProLocalizer _talantsText;
    [SerializeField] private Button _headerButton;

    [SerializeField] private bool _isMainMenu = true;

    private List<UIMenuMainTalentsPanelGroup> ItemsPool = new();

    private bool _isCollapsed;
    private readonly Dictionary<UIMenuMainTalentsPanelGroup, bool> _savedGroupStates = new();

    private TalentSystem _talentSystem;

    private void Awake()
    {
        if (_headerButton != null)
            _headerButton.onClick.AddListener(ToggleWholePanel);
    }

    public void ToggleWholePanel()
    {
        bool anyExpanded = false;
        foreach (var group in ItemsPool)
        {
            if (group != null && group.IsExpanded)
            {
                anyExpanded = true;
                break;
            }
        }

        bool expand = !anyExpanded;

        foreach (var group in ItemsPool)
        {
            if (group == null) continue;
            group.SetItemsExpanded(expand);
        }
    }

    public void Show(TalentSystem talentSystem, bool isGameUI, bool isInteractable = true)
    {
        ResetPanel();

        _isCollapsed = false;
        _savedGroupStates.Clear();

        _talentSystem = talentSystem;

        if (!_isMainMenu)
        {
            if (_talentSystem.Level != null) _talentSystem.Level.LVLUped += OnLevelUp;
        }

        else LevelCharacterManager.Instance.OnLevelChanged += OnLevelUp;

        SaveManager.Instance.TalentStateSettled -= UpdateTalentPointsText;
        SaveManager.Instance.TalentStateSettled += UpdateTalentPointsText;

        foreach (var data in _talentSystem.TalentsGroups)
        {
            var panel = Instantiate(_talentsPanelGroup, _itemsParent);

            panel.SetPanel(data, _talentSystem, _attributesPanel, isGameUI, isInteractable);

            //panel.OnShowPanelGroup += HidePanels;
            panel.PointerEnteredOnTalentIcon += ShowTalentInfo;
            panel.PointerExitedOnTalentIcon += HideTalentInfo;
            panel.OnTalentChanged += UpdateTalentPointsText;
            panel.OnAnyTalentChanged += RefreshAllGroups;

            ItemsPool.Add(panel);
        }

        UpdateTalentPointsText();
    }

    private void OnEnable()
    {
        SaveManager.Instance.TalentStateSettled += UpdateTalentPointsText;
    }

    private void OnDisable()
    {
        foreach (var item in ItemsPool)
        {
            item.OnShowPanelGroup -= HidePanels;
            item.PointerEnteredOnTalentIcon -= ShowTalentInfo;
            item.PointerExitedOnTalentIcon -= HideTalentInfo;
            item.OnTalentChanged -= UpdateTalentPointsText;
            item.OnAnyTalentChanged -= RefreshAllGroups;
        }

        SaveManager.Instance.TalentStateSettled -= UpdateTalentPointsText;

        if (!_isMainMenu)
        {
            if (_talentSystem != null)
                if (_talentSystem.Level != null) _talentSystem.Level.LVLUped -= OnLevelUp;
        }

        else LevelCharacterManager.Instance.OnLevelChanged -= OnLevelUp;
    }

    private void OnLevelUp(int newLevel)
    {
        if (this == null || gameObject == null) return;
        if (!isActiveAndEnabled) return;

        UpdateTalentPointsText();
    }

    private void RefreshAllGroups()
    {
        foreach (var group in ItemsPool)
            group.RefreshRowsLocked();
    }

    private void UpdateTalentPointsText()
    {
        if (this == null || gameObject == null) return;
        if (_talantsText == null) return;
        if (_talentSystem == null) return;

        int maxPoints = LevelCharacterManager.Instance.GetCurrentLevel();

        int usedPoints = _talentSystem.GetActiveTalentCount();
        int freePoints = maxPoints - usedPoints;

        if (_talentSystem.Points >= 0)
        {
            if (_talantsText != null)
                _talantsText.gameObject.SetActive(true);

            if (!_isMainMenu)
                _talantsText.ChangeKey(_talentSystem.Points);
            else
            {
                _talentSystem.SetPoints(freePoints);
                _talantsText.ChangeKey(_talentSystem.Points);
            }
        }
        else
        {
            if (_talantsText != null)
                _talantsText.gameObject.SetActive(false);
        }

        if (_attributesPanel != null)
            _attributesPanel.UpdateAttributesPoints();
    }

    private void ResetPanel()
    {
        if (ItemsPool.Count <= 0) return;

        foreach (var attribute in ItemsPool)
        {
            attribute.Destroy();
        }
        ItemsPool.Clear();
    }

    public void HidePanels()
    {
        foreach (var item in ItemsPool)
        {
            item.Hide();
        }
    }

    private void ShowTalentInfo(TalentData data, string rowCondition)
    {
        _talentInfoPanel.Show(data, rowCondition);
    }

    private void HideTalentInfo(TalentData data)
    {
        _talentInfoPanel.Hide();
    }
}