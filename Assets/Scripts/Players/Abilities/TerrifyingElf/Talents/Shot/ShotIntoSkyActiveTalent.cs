using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShotIntoSkyActiveTalent : Talent
{
    [SerializeField] private ShotIntoSky shotIntoSky;
    [SerializeField] private SkillManager _ability;

    public override void Enter()
    {
        _ability.ActivateSkill(shotIntoSky);
    }

    public override void Exit()
    {
        _ability.DeactivateSkill(shotIntoSky);
    }
}
