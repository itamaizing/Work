using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbsorptionBallTalent : Talent
{
    [SerializeField] private AbsorptionBall _absorptionBall;
    [SerializeField] private SkillManager _ability;

    public override void Enter()
    {
        _ability.ActivateSkill(_absorptionBall);
    }

    public override void Exit()
    {
        _ability.DeactivateSkill(_absorptionBall);
    }
}
