using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NatureTalent_7 : Talent
{
    [SerializeField] private SleepSpell sleep;
    [SerializeField] private SkillManager _ability;
    [SerializeField] private ShotIntoSky shotIntoSky;
    [SerializeField] private ShotsIntoSky shotsIntoSky;

    public override void Enter()
    {
        _ability.ActivateSkill(sleep);
        shotIntoSky.SetSilenceTalentActive(true);
        shotsIntoSky.SetSilenceTalentActive(true);
    }

    public override void Exit()
    {
        _ability.DeactivateSkill(sleep);
        shotIntoSky.SetSilenceTalentActive(false);
        shotsIntoSky.SetSilenceTalentActive(false);
    }
}
