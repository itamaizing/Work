using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerIconMain : PlayerIcon, IPointerExitHandler, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] private LvlInfo _lvlInfo;
    [SerializeField] private Image _FrameBlink;
    [SerializeField] private bool _openTalentsOnClick = true;
    public static event Action<HeroComponent, bool> HeroPortraitClicked;

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
        if (!_openTalentsOnClick || eventData.button != PointerEventData.InputButton.Left) return;

        var target = Character != null ? Character : _shownCharacter;
        var local = Character.Local;
        if (target == null || target is not HeroComponent hero || local == null) return;

        bool isAllied = hero == local ||
            (local.NetworkSettings != null && hero.NetworkSettings != null &&
             local.RelationTo(hero) == CharacterRelation.Ally);
        HeroPortraitClicked?.Invoke(hero, isAllied);
    }
}
