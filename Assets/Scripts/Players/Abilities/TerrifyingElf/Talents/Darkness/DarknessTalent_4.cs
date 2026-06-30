using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarknessTalent_4 : Talent
{
    [SerializeField] private Ghost ghost;
    [SerializeField] private SkillManager skillManager;

    public override void Enter()
    {
        //ghost.SendingGhostTargetTalentActive(true);
        //skillManager.ActivateSkill(ghost);
    }

    public override void Exit()
    {
        //ghost.SendingGhostTargetTalentActive(false);
        //skillManager.DeactivateSkill(ghost);
    }
}
