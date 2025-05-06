using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeriesPhysicalTalent : Talent
{
    [SerializeField] private PhysicalAttack physicalAttack;
    [SerializeField] private IceShadow iceShadow;
    [SerializeField] private IceRolling iceRolling;
    [SerializeField] private SkillManager skillManager;

    public override void Enter()
    {
        physicalAttack.SeriesPhysicalTalentActive(true);
        skillManager.ActivateSkill(iceShadow);
        skillManager.ActivateSkill(iceRolling);
    }

    public override void Exit()
    {
        physicalAttack.SeriesPhysicalTalentActive(false);
        skillManager.DeactivateSkill(iceShadow);
        skillManager.DeactivateSkill(iceRolling);
    }
}
