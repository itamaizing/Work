using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NatureTalent_7 : Talent
{
    [SerializeField] private PullingHealth pullingHealth;
    [SerializeField] private SkillManager ability;

    public override void Enter()
    {
        ability.ActivateSkill(pullingHealth);
    }

    public override void Exit()
    {
        ability.DeactivateSkill(pullingHealth);
    }
}
