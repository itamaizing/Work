using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HuntressTalent_13 : Talent
{
    [SerializeField] private TerrifyingElfAura terrifyingElfAura;

    public override void Enter()
    {
        terrifyingElfAura.ThirdShotRow(true);
    }

    public override void Exit()
    {
        terrifyingElfAura.ThirdShotRow(false);
    }
}
