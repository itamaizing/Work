using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightTalent_2 : Talent
{
    [SerializeField] private EmeraldSkin _emeraldSkin;

    public override void Enter()
    {
        _emeraldSkin.EnableTalentLightMagicBoost(true);
    }

    public override void Exit()
    {
        _emeraldSkin.EnableTalentLightMagicBoost(false);
    }
}
