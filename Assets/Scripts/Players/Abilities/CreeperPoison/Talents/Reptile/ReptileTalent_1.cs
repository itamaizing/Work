using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReptileTalent_1 : Talent
{
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private LightningStrikes lightningStrikes;
    [SerializeField] private CreeperInvisible creeperInvisible;

    public override void Enter()
    {
        skillManager.ActivateSkill(lightningStrikes);
        skillManager.ActivateSkill(creeperInvisible);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(lightningStrikes);
        skillManager.DeactivateSkill(creeperInvisible);
    }
}
