using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InviteRequestUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;
    [SerializeField] private Button _acceptButton;
    [SerializeField] private Button _cancelButton;

    private int _inviterId;

    public event Action<bool, int> Accepted;

    private void Awake()
    {
        _acceptButton.onClick.AddListener(OnAccept);
        _cancelButton.onClick.AddListener(OnCancel);
    }

    private void OnDestroy()
    {
        _acceptButton.onClick.RemoveListener(OnAccept);
        _cancelButton.onClick.RemoveListener(OnCancel);
    }

    public void SetInvater(string name, int id)
    {
        _text.text = $"Вас пригласил {name}";
        _inviterId = id;
    }

    private void OnAccept()
    {
        Accepted?.Invoke(true, _inviterId);
        gameObject.SetActive(false);
    }

    private void OnCancel()
    {
        Accepted?.Invoke(false, _inviterId);
        gameObject.SetActive(false);
    }

}
