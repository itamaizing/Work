using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NatureTalent_5 : Talent
{
    [SerializeField] private TerrifyingElfAura terrifyingElfAura;
    [SerializeField] private SleepSpell sleep;

    public override void Enter()
    {
        terrifyingElfAura.TreeRadiusCalmessTalentActive(true);
        sleep.SleepInnerDarknessTalent(true);
    }

    public override void Exit()
    {
        terrifyingElfAura.TreeRadiusCalmessTalentActive(false);
        sleep.SleepInnerDarknessTalent(true);
    }
}
