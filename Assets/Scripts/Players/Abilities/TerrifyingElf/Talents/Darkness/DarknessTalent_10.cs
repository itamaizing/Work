using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarknessTalent_10 : Talent
{
    [SerializeField] private TerrifyingElfAura terrifyingElfAura;

    public override void Enter()
    {
        terrifyingElfAura.SuppressionManaAbsorption(true);
    }

    public override void Exit()
    {
        terrifyingElfAura.SuppressionManaAbsorption(false);
    }
}
