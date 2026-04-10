using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaPoisonTalent_5 : Talent
{
    [SerializeField] private BlockPassiveSkill block;
    [SerializeField] private SkillManager manager;

    public override void Enter()
    {
        manager.ActivateSkill(block);
    }

    public override void Exit()
    {
        manager.DeactivateSkill(block);
    }
}