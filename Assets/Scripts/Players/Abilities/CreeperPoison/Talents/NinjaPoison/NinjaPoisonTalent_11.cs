using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaPoisonTalent_11 : Talent
{
    [SerializeField] private BlockPassiveSkill block;

    public override void Enter()
    {
        block.MagicOrPhysicRessist(true);
    }

    public override void Exit()
    {
        block.MagicOrPhysicRessist(false);
    }
}