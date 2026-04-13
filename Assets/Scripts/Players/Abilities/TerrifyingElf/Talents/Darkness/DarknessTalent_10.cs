using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarknessTalent_10 : Talent
{
    [SerializeField] private Silence silence;
    [SerializeField] private TerrifyingElfAura terrifyingElfAura;

    public override void Enter()
    {
        silence.SilenceAddAllCharacterWithDeabaffElf(true);
        terrifyingElfAura.SuppressionManaAbsorption(true);
    }

    public override void Exit()
    {
        silence.SilenceAddAllCharacterWithDeabaffElf(false);
        terrifyingElfAura.SuppressionManaAbsorption(false);
    }
}
