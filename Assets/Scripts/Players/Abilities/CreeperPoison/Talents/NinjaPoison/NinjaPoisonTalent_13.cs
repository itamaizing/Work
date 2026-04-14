using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaPoisonTalent_13 : Talent
{
    [SerializeField] private ProtectiveScales protectiveScales;
    [SerializeField] private SkillManager skillManager;

    public override void Enter()
    {
        skillManager.ActivateSkill(protectiveScales);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(protectiveScales);
    }
}