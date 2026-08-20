using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class UIMenuMainTalentsPanel : MonoBehaviour
{
    [SerializeField] private UIMenuMainAttributesPanel _attributesPanel;
    [SerializeField] private UIMenuMainTalentsPanelGroup _talentsPanelGroup;
    [SerializeField] private RectTransform _itemsParent;
    [SerializeField] private TalentInfoPanel _talentInfoPanel;
    [SerializeField] private TMProLocalizer _talantsText;

    [SerializeField] private bool _isMainMenu = true;

    private List<UIMenuMainTalentsPanelGroup> ItemsPool = new();

    private TalentSystem _talentSystem;

    public void Show(TalentSystem talentSystem, bool isGameUI, bool isInteractable = true)
    {
        Debug.LogError("TryShow");
        
        ResetPanel();

        _talentSystem = talentSystem;

        if (!_isMainMenu)
        {
            if (_talentSystem.Level != null) _talentSystem.Level.LVLUped += OnLevelUp;
        }

        else LevelCharacterManager.Instance.OnLevelChanged += OnLevelUp;

        foreach (var data in _talentSystem.TalentsGroups)
        {
            var panel = Instantiate(_talentsPanelGroup, _itemsParent);

            panel.SetPanel(data, _attributesPanel, isGameUI, isInteractable);

            panel.OnShowPanelGroup += HidePanels;
            panel.PointerEnteredOnTalentIcon += ShowTalentInfo;
            panel.PointerExitedOnTalentIcon += HideTalentInfo;
            panel.OnTalentChanged += UpdateTalentPointsText;

            ItemsPool.Add(panel);
        }

        UpdateTalentPointsText();
    }

    private void OnDisable()
    {
        foreach (var item in ItemsPool)
        {
            item.OnShowPanelGroup -= HidePanels;
            item.PointerEnteredOnTalentIcon -= ShowTalentInfo;
            item.PointerExitedOnTalentIcon -= HideTalentInfo;
            item.OnTalentChanged -= UpdateTalentPointsText;
        }

        if (!_isMainMenu)
        {
            if(_talentSystem != null)
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

    private void UpdateTalentPointsText()
{
    if (this == null || gameObject == null) return;
    if (_talantsText == null) return;
    if (_talentSystem == null) return;

    int maxPoints = LevelCharacterManager.Instance.GetCurrentLevel() + 10;

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

    private void ShowTalentInfo(TalentData data)
    {
        _talentInfoPanel.Show(data);
    }

    private void HideTalentInfo(TalentData data)
    {
        _talentInfoPanel.Hide();
    }
}
