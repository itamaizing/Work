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

    private List<UIMenuMainTalentsPanelGroup> ItemsPool = new();
    
    private TalentSystem _talentSystem;

    public void Show(TalentSystem talentSystem, bool isGameUI, bool isInteractable = true)
    {
        ResetPanel();
        
        _talentSystem = talentSystem;

        if (_talentSystem.Level != null) _talentSystem.Level.LVLUped += OnLevelUp;

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
        if(_talentSystem != null)
            if (_talentSystem.Level != null) _talentSystem.Level.LVLUped -= OnLevelUp;
    }

    private void OnLevelUp(int newLevel)
    {
        UpdateTalentPointsText();
    }

    private void UpdateTalentPointsText()
    {
        if (_talentSystem.Points > 0)
        {
            _talantsText.gameObject.SetActive(true);
            _talantsText.ChangeKey(_talentSystem.Points);
        }
        else
        {
            _talantsText.gameObject.SetActive(false);
        }

        Debug.Log($"_points: {_talentSystem.Points}");
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
