using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrostTalent_2 : Talent
{
    [SerializeField] private IcePuddle icePuddle;
    [SerializeField] IceShadow iceShadow;
    [SerializeField] private SkillManager skillManager;

    public override void Enter()
    {
        skillManager.ActivateSkill(iceShadow);
        skillManager.ActivateSkill(icePuddle);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(iceShadow);
        skillManager.DeactivateSkill(icePuddle);
    }
}
