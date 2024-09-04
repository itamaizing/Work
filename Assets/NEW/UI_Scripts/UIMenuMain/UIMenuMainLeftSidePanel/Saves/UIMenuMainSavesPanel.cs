using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class UIMenuMainSavesPanel : MonoBehaviour
{
    [ReadOnly,ShowInInspector]
    public UIMenuMainWindow Owner;
    
    [SerializeField] private UIMenuMainSavePanelItem savePanelItemItem;
    
    [SerializeField] private RectTransform _itemsParent;
    
    private List<UIMenuMainSavePanelItem> ItemsPool = new();

    private int _currentActiveIndex = 0;
    
    public void Show()
    {
        for (int i = 0; i < 3; i++)
        {
           var item = Instantiate(savePanelItemItem, _itemsParent);
           item.Owner = this;
           item.Fill(i + 1);
           ItemsPool.Add(item);
        }
    }

    public void Select(int index)
    {
        _currentActiveIndex = _currentActiveIndex ==  index ? 0 : index;
        Owner.SetHeroSaveIndex(_currentActiveIndex);
    }
}
