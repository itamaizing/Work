using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIMenuMainTalentsPanel : MonoBehaviour
{
    [System.Serializable]
    private sealed class TalentTab
    {
        public Button Button;
        public TMP_Text Label;
        public UIMenuMainTalentsPanelGroup Page;
    }

    [SerializeField] private UIMenuMainAttributesPanel _attributesPanel;
    [SerializeField] private UIMenuMainTalentsPanelGroup _talentsPanelGroup;
    [SerializeField] private RectTransform _itemsParent;
    [SerializeField] private TalentInfoPanel _talentInfoPanel;
    [SerializeField] private TMProLocalizer _talantsText;
    [SerializeField] private Button _headerButton;

    [SerializeField] private GameObject _tabs;
    [SerializeField] private GameObject _pages;
    [SerializeField] private TalentTab[] _branchTabs;
    [SerializeField] private Color _selectedTabBackground = new Color32(30, 30, 30, 255);
    [SerializeField] private Color _tabBackground = new Color32(47, 47, 47, 255);
    [SerializeField] private Color _selectedTabText = new Color32(255, 216, 54, 255);
    [SerializeField] private Color _tabText = new Color32(165, 140, 55, 255);
    [SerializeField] private bool _isMainMenu = true;

    private List<UIMenuMainTalentsPanelGroup> ItemsPool = new();

    private readonly Dictionary<TalentSystem, int> _branchByHero = new();
    private int _selectedBranch;
    private bool _isCollapsed;

    private TalentSystem _talentSystem;
    private bool _isInteractable;

    private bool UseTabs => !_isMainMenu && _tabs != null;

    private void Awake()
    {
        if (_headerButton != null)
            _headerButton.onClick.AddListener(ToggleWholePanel);

        if (UseTabs)
            UIMenuMainAttributesPanel.MoveTooltipOutsideScroll(this, _talentInfoPanel);
    }

    public void ToggleWholePanel()
    {
        if (UseTabs)
        {
            _isCollapsed = !_isCollapsed;
            ApplySelectedBranch();
            return;
        }

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
        if (UseTabs && _talentSystem == talentSystem && _isInteractable == isInteractable)
        {
            UpdateTalentPointsText();
            return;
        }

        UnsubscribeFromLevel();
        ResetPanel();

        _talentSystem = talentSystem;
        _isInteractable = isInteractable;

        if (isActiveAndEnabled) SubscribeToLevel();

        var branches = _talentSystem.TalentsGroups;
        int branchCount = UseTabs ? Mathf.Min(branches.Count, _branchTabs.Length) : branches.Count;
        for (int i = 0; i < branchCount; i++)
        {
            var panel = UseTabs ? _branchTabs[i].Page : Instantiate(_talentsPanelGroup, _itemsParent);
            panel.SetPanel(branches[i], _talentSystem, _attributesPanel, isGameUI, isInteractable);
            panel.PointerEnteredOnTalentIcon += ShowTalentInfo;
            panel.PointerExitedOnTalentIcon += HideTalentInfo;
            panel.OnTalentChanged += UpdateTalentPointsText;
            panel.OnAnyTalentChanged += RefreshAllGroups;

            ItemsPool.Add(panel);
        }

        if (UseTabs)
        {
            _selectedBranch = 0;
            if (_branchByHero.TryGetValue(_talentSystem, out int branchId))
            {
                int index = branches.FindIndex(group => group.ID == branchId);
                if (index >= 0 && index < branchCount) _selectedBranch = index;
            }
            ApplySelectedBranch();
        }

        UpdateTalentPointsText();
    }

    public void SelectBranch(int index)
    {
        if (!UseTabs || index < 0 || index >= ItemsPool.Count) return;

        _selectedBranch = index;
        _branchByHero[_talentSystem] = _talentSystem.TalentsGroups[index].ID;
        ApplySelectedBranch();
    }

    private void ApplySelectedBranch()
    {
        if (_talentInfoPanel != null) _talentInfoPanel.Hide();

        bool expanded = !_isCollapsed && ItemsPool.Count > 0;
        _tabs.SetActive(expanded);
        _pages.SetActive(expanded);

        for (int i = 0; i < ItemsPool.Count; i++)
            ItemsPool[i].SetGroupVisible(i == _selectedBranch && expanded);

        for (int i = 0; i < _branchTabs.Length; i++)
        {
            var tab = _branchTabs[i];
            bool selected = i == _selectedBranch;
            tab.Button.gameObject.SetActive(i < ItemsPool.Count);
            tab.Button.image.color = selected ? _selectedTabBackground : _tabBackground;
            tab.Label.color = selected ? _selectedTabText : _tabText;
        }
    }

    private void OnEnable()
    {
        SaveManager.Instance.TalentStateSettled += UpdateTalentPointsText;
        SubscribeToLevel();
    }

    private void OnDisable()
    {
        SaveManager.Instance.TalentStateSettled -= UpdateTalentPointsText;
        UnsubscribeFromLevel();
        if (UseTabs && _talentInfoPanel != null) _talentInfoPanel.Hide();
    }

    private void SubscribeToLevel()
    {
        if (_talentSystem == null) return;
        if (_isMainMenu)
            LevelCharacterManager.Instance.OnLevelChanged += OnLevelUp;
        else if (_talentSystem.Level != null)
            _talentSystem.Level.LVLUped += OnLevelUp;
    }

    private void UnsubscribeFromLevel()
    {
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
        if (_talentSystem == null) return;

        for (int i = 0; UseTabs && i < ItemsPool.Count; i++)
        {
            var branch = _talentSystem.TalentsGroups[i];
            _branchTabs[i].Label.text = $"{branch.Name}";
        }

        if (_talantsText == null) return;

        int maxPoints = LevelCharacterManager.Instance.GetCurrentLevel();

        int usedPoints = _talentSystem.GetActiveTalentCount();
        int freePoints = maxPoints - usedPoints;

        if (_talentSystem.Points >= 0)
        {
            if (_talantsText != null)
                _talantsText.gameObject.SetActive(true);

            if (!_isMainMenu || !_isInteractable)
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
        foreach (var group in ItemsPool)
        {
            group.PointerEnteredOnTalentIcon -= ShowTalentInfo;
            group.PointerExitedOnTalentIcon -= HideTalentInfo;
            group.OnTalentChanged -= UpdateTalentPointsText;
            group.OnAnyTalentChanged -= RefreshAllGroups;
            group.SetGroupVisible(false);
            if (UseTabs) group.ClearPage();
            else group.Destroy();
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
