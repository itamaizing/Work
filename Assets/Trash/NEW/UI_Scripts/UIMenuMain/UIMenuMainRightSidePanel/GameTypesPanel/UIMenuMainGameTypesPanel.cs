using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class UIMenuMainGameTypesPanel : MonoBehaviour
{
    [ReadOnly,ShowInInspector]
    public UIMenuMainWindow Owner;
    
    [SerializeField] private List<UIMenuMainGameTypesPanelMainTypeItem> gameTypeMainTypeItem;
    [SerializeField] private List<UIMenuMainGameTypesPanelCountTypeItem> gameTypeCountTypeItem;

	[SerializeField] private RectTransform _mainItemsParent;
    [SerializeField] private RectTransform _countItemsParent;
    
    private List<UIMenuMainGameTypesPanelMainTypeItem> _mainGameTypes = new();
    private List<UIMenuMainGameTypesPanelCountTypeItem> _countGameTypes = new();


	public void Show()
    {
        if(Owner == null) return;

		 foreach (var item in gameTypeMainTypeItem)
		 {
			 item.Owner = this;
			 item.Fill();
			 item.Selected += OnMainModeSelected;
			 _mainGameTypes.Add(item);
		 }

		 foreach (var item in gameTypeCountTypeItem)
		 {
			 item.Owner = this;
			 item.Fill();
			 item.Selected += OnCountModeSelected;
			 _countGameTypes.Add(item);
		 }
	}

	private void OnCountModeSelected(GameMode mode)
    {
        ServerManager.Instance.SetMode(mode);
    }
    
    public void OnMainModeSelected(MainGameMode mode)
    {
        
    }
}
