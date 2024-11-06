using System.Collections.Generic;
using UnityEngine;

public class UIMenuMainTalentsPanel : MonoBehaviour
{
    [SerializeField] private UIMenuMainAttributesPanel _attributesPanel;
    [SerializeField] private UIMenuMainTalentsPanelGroup _talentsPanelGroup;
    [SerializeField] private RectTransform _itemsParent;
    
    private List<UIMenuMainTalentsPanelGroup> ItemsPool = new();
    
    private TalentSystem _talentSystem;

    public void Show(TalentSystem talentSystem, bool isGameUI)
    {
        ResetPanel();
        
        _talentSystem = talentSystem;

        foreach (var data in _talentSystem.Talents)
        {
            var panel = Instantiate(_talentsPanelGroup, _itemsParent);
            
            panel.SetPanel(data, _attributesPanel, isGameUI);
            panel.OnShowPanelGroup += HidePanels;
            
            ItemsPool.Add(panel);
        }
    }

    private void OnDisable()
    {
        foreach (var item in ItemsPool)
        {
            item.OnShowPanelGroup -= HidePanels;
        }
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
}
