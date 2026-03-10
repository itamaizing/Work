using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReptileTalent_1 : Talent
{
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private LightningStrikes lightningStrikes;

    public override void Enter()
    {
        skillManager.ActivateSkill(lightningStrikes);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(lightningStrikes);
    }
}
