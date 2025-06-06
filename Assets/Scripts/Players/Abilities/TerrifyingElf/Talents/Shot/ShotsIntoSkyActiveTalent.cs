using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShotsIntoSkyActiveTalent : Talent
{
    [SerializeField] private ShotsIntoSky shotsIntoSky;
    [SerializeField] private SkillManager _ability;

    public override void Enter()
    {
        _ability.ActivateSkill(shotsIntoSky);
    }

    public override void Exit()
    {
        _ability.DeactivateSkill(shotsIntoSky);
    }
}
