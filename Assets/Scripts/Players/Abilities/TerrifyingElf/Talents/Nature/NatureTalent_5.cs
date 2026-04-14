using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NatureTalent_5 : Talent
{
    [SerializeField] private TerrifyingElfAura terrifyingElfAura;

    public override void Enter()
    {
        terrifyingElfAura.TreeRadiusCalmessTalentActive(true);
    }

    public override void Exit()
    {
        terrifyingElfAura.TreeRadiusCalmessTalentActive(false);
    }
}
