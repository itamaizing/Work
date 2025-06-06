using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CalmnessTalent : Talent
{
    [SerializeField] private TerrifyingElfAura terrifyingElfAura;

    public override void Enter()
    {
        terrifyingElfAura.CalmnessTalentActive(true);
    }

    public override void Exit()
    {
        terrifyingElfAura.CalmnessTalentActive(false);
    }
}
