using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundTrapActiveTalent : Talent
{
    [SerializeField] private GroundTrap groundTrap;
    [SerializeField] private SkillManager _ability;

    public override void Enter()
    {
        _ability.ActivateSkill(groundTrap);
    }

    public override void Exit()
    {
        _ability.DeactivateSkill(groundTrap);
    }
}
