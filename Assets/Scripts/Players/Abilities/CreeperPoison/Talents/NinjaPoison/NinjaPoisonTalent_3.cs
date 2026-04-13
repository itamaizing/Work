using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaPoisonTalent_3 : Talent
{
    [SerializeField] private CreeperInvisible creeperInvisible;
    [SerializeField] private SkillManager manager;

    public override void Enter()
    {
        manager.ActivateSkill(creeperInvisible);
    }

    public override void Exit()
    {
        manager.DeactivateSkill(creeperInvisible);
    }
}

