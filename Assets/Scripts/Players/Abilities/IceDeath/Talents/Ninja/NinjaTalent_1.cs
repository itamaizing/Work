using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaTalent_1 : Talent
{
    [SerializeField] private IceRolling iceRolling;
    [SerializeField] private SkillManager manager;

    public override void Enter()
    {
        manager.ActivateSkill(iceRolling);
    }

    public override void Exit()
    {
        manager.DeactivateSkill(iceRolling);
    }
}
