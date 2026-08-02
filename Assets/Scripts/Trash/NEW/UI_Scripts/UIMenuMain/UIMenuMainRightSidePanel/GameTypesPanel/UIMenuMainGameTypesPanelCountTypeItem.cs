using Mirror;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIMenuMainGameTypesPanelCountTypeItem : MonoBehaviour
{
    public event UnityAction<GameMode> Selected;

    [SerializeField] private Button _button;
    [SerializeField] private TMProLocalizer _itemTitle;
    [SerializeField] private GameMode _itemMode;
    [SerializeField] private ServerManager _serverManager;

    private void OnEnable()
    {
        _button.onClick.AddListener(OnClick);
    }

    private void OnDisable()
    {
        _button.onClick.RemoveListener(OnClick);
    }

    private void OnClick()
    {
        MPNetworkManager.Instance.CurrentGameMode = _itemMode;
        _serverManager.SetMode(_itemMode);
    }

    public void Fill()
    {
        _itemTitle.Localize(_itemMode.ToString());
    }

    public void Select()
    {
        Selected?.Invoke(_itemMode);
    }
}
