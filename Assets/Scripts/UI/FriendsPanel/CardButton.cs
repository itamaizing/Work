using System;
using UnityEngine;
using UnityEngine.UI;

public class CardButton : MonoBehaviour, IDisposable
{
    [SerializeField] private PlayerCardUI _cardUI;
    [SerializeField] private Button _button;

    public Button Button { get => _button; }

    public event Action<PlayerCardUI> Action;

    private void Awake()
    {
        _button.onClick.AddListener(OnClick);
    }

    public void Init(PlayerCardUI cardUI)
    {
        _cardUI = cardUI;
    }

    public void SetAction(Action<PlayerCardUI> action)
    {
        Action += action;
    }

    private void OnClick()
    {
        Action?.Invoke(_cardUI);
    }

    public void Dispose()
    {
        Action = null;
    }
}
