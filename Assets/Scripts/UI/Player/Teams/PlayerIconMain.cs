using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIconMain : PlayerIcon
{
    [SerializeField] private LvlInfo _lvlInfo;

    protected override void UpdateInfo(Character character)
    {
        base.UpdateInfo(character);
        _lvlInfo.Init(character.LVL);
    }
}
