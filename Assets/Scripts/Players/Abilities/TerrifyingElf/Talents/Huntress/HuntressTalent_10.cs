using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HuntressTalent_10 : Talent
{
    [SerializeField] private TerrifyingElfAura terrifyingElfAura;

    public override void Enter()
    {
        terrifyingElfAura.ElvenSkillPhysDamageHealthChance(true);
    }

    public override void Exit()
    {
        terrifyingElfAura.ElvenSkillPhysDamageHealthChance(false);
    }
}
