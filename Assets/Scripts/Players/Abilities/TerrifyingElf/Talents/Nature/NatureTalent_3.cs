using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NatureTalent_3 : Talent
{
    [SerializeField] private TerrifyingElfAura terrifyingElfAura;
    [SerializeField] private GrowTree growTree;

    public override void Enter()
    {
        terrifyingElfAura.CalmnessTalentActive(true);
        growTree.ShotTreeCooldownTalent(true);
    }

    public override void Exit()
    {
        terrifyingElfAura.CalmnessTalentActive(false);
        growTree.ShotTreeCooldownTalent(false);
    }
}
