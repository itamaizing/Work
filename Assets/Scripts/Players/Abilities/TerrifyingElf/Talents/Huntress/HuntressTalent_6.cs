using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HuntressTalent_6 : Talent
{
    [SerializeField] private TerrifyingElfAura terrifyingElfAura;

    public override void Enter()
    {
        terrifyingElfAura.ElvenSkillTalent(true);
    }

    public override void Exit()
    {
        terrifyingElfAura.ElvenSkillTalent(false);
    }
}
