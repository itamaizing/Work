using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerIconMain : PlayerIcon, IPointerExitHandler, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] private LvlInfo _lvlInfo;
    [SerializeField] private Image _FrameBlink;
    public static event Action<Character> OwnIconClicked;

    private Character _shownCharacter;

    public void OnPointerEnter(PointerEventData eventData)
    {
        _FrameBlink.gameObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _FrameBlink.gameObject.SetActive(false);
    }

    protected override void UpdateInfo(Character character)
    {
        _shownCharacter = character;
        base.UpdateInfo(character);
        if (_lvlInfo != null)
            _lvlInfo.Init(character.LVL);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        var target = Character != null ? Character : _shownCharacter;
        if (target == null) return;

        if (target == Character.Local)
            OwnIconClicked?.Invoke(target);
        else
            TargetSelector.Instance?.SelectTarget(target);
    }
}