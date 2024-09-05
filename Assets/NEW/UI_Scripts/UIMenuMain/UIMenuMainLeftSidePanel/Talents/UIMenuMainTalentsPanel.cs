using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class UIMenuMainTalentsPanel : MonoBehaviour
{
    [ReadOnly,ShowInInspector]
    public UIMenuMainWindow Owner;
    
    [SerializeField] private UIMenuMainTalentsPanelGroup _talentsPanelGroup;
    
    [SerializeField] private RectTransform _itemsParent;
    
    private List<UIMenuMainTalentsPanelGroup> ItemsPool = new();

    public void Show()
    {
        if(Owner == null) return;
        
        ResetPanel();

        foreach (var data in Owner.GetHero().Data.Talents)
        {
            var panel = Instantiate(_talentsPanelGroup, _itemsParent);
            
            panel.Owner = this;
            panel.SetPanel(data);
            
            ItemsPool.Add(panel);
        }
    }
    
    private void ResetPanel()
    {
        if (ItemsPool.Count > 0)
        {
            foreach (var attribute in ItemsPool)
            {
                attribute.Destroy();
            }
            ItemsPool.Clear();
        }

        ItemsPool = new();
    }

    public void HidePanels()
    {
        foreach (var item in ItemsPool)
        {
            item.Hide();
        }
    }
}
