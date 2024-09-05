using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class UIMenuMainGameTypesPanelMainTypeItem : MonoBehaviour
{
    public event UnityAction<MainGameMode> Selected;
    
    [ReadOnly,ShowInInspector]
    public UIMenuMainGameTypesPanel Owner;
    
    [SerializeField] private TMProLocalizer _itemTitle;

    private MainGameMode _itemMode;
    
    public void Fill(MainGameMode modeName)
    {
        _itemTitle.Localize(modeName.ToString());
        _itemMode = modeName;
    }

    public void Select()
    {
        Selected?.Invoke(_itemMode);
    }
}
