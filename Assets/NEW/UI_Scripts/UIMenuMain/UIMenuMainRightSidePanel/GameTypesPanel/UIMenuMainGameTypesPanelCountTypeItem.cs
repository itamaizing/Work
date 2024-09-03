using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class UIMenuMainGameTypesPanelCountTypeItem : MonoBehaviour
{
    public event UnityAction<GameMode> Selected;
    
    [ReadOnly,ShowInInspector]
    public UIMenuMainGameTypesPanel Owner;
    
    [SerializeField] private TMProLocalizer _itemTitle;

    private GameMode _itemMode;
    
    public void Fill(GameMode modeName)
    {
        _itemTitle.Localize(modeName.ToString());
        _itemMode = modeName;
    }

    public void Select()
    {
        Debug.Log(_itemMode);
        
        Selected?.Invoke(_itemMode);
    }
}
