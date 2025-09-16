using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaTalent_3 : Talent
{
    [SerializeField] IceShadow iceShadow;
    [SerializeField] IceSword iceSword;
    [SerializeField] SkillManager manager;

    public override void Enter()
    {
        manager.ActivateSkill(iceShadow);
        manager.ActivateSkill(iceSword);
    }

    public override void Exit()
    {
        manager.DeactivateSkill(iceShadow);
        manager.DeactivateSkill(iceSword);
    }
}
