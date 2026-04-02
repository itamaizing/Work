using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ReptileTalent_4 : Talent
{
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private MetabolismReptile _metabolismReptile;
    [SerializeField] private LightningMovement lightningMovement;

    public override void Enter()
    {
        skillManager.ActivateSkill(_metabolismReptile);
        skillManager.ActivateSkill(lightningMovement);

    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(_metabolismReptile);
        skillManager.DeactivateSkill(lightningMovement);
    }
}

