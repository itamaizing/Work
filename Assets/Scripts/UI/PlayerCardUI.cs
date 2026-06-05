using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PlayerCardUI : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _name;
    [SerializeField] private CardButton[] _buttons;

    public int Id { get; private set; }

    public void Init(string name, int id)
    {
        _name.text = name;
        Id = id;
    }

    private void OnDestroy()
    {
        foreach (var button in _buttons)
        {
            button.Dispose();
        }
    }

    public void SetButtn(int index, Sprite sprite, Action<PlayerCardUI> action)
    {
        _buttons[index].gameObject.SetActive(true);
        _buttons[index].SetAction(action);
        _buttons[index].Button.image.sprite = sprite;
    }
}