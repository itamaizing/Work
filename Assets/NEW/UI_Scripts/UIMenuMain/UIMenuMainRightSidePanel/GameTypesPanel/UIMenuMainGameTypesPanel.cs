using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class UIMenuMainGameTypesPanel : MonoBehaviour
{
    [ReadOnly,ShowInInspector]
    public UIMenuMainWindow Owner;
    
    [SerializeField] private UIMenuMainGameTypesPanelMainTypeItem gameTypeMainTypeItem;
    [SerializeField] private UIMenuMainGameTypesPanelCountTypeItem gameTypeCountTypeItem;
    
    [SerializeField] private RectTransform _mainItemsParent;
    [SerializeField] private RectTransform _countItemsParent;
    
    private List<UIMenuMainGameTypesPanelMainTypeItem> _mainGameTypes = new();
    private List<UIMenuMainGameTypesPanelCountTypeItem> _countGameTypes = new();
    
    public void Show()
    {
        if(Owner == null) return;
        
        var activeMainModes = MultiplayerManager.Instance.ActiveMainModes;
        var activeCountModes = MultiplayerManager.Instance.ActiveCountModes;

        foreach (var item in activeMainModes)
        {
            var mode = Instantiate(gameTypeMainTypeItem, _mainItemsParent);
            mode.Owner = this;
            mode.Fill(item);
            mode.Selected += OnMainModeSelected;
            _mainGameTypes.Add(mode);
        }
        
        foreach (var item in activeCountModes)
        {
            var mode = Instantiate(gameTypeCountTypeItem, _countItemsParent);
            mode.Owner = this;
            mode.Fill(item);
            mode.Selected += OnCountModeSelected;
            _countGameTypes.Add(mode);
        }
    }

    public void OnCountModeSelected(GameMode mode)
    {
        MultiplayerManager.Instance.SetMode(mode);
    }
    
    public void OnMainModeSelected(MainGameMode mode)
    {
        
    }
}
