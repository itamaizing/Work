using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathTalent_1 : Talent
{
    [SerializeField] private SeriesOfStrikes _seriesOfStrikes;
    [SerializeField] private SkillManager _skillManager;
    [SerializeField] private PortalDarkness _portalDarkness;

    public override void Enter()
    {
        _seriesOfStrikes.IceRuneTalentActive(true);
        _skillManager.ActivateSkill(_portalDarkness);
    }

    public override void Exit()
    {
        _seriesOfStrikes.IceRuneTalentActive(false);
        _skillManager.DeactivateSkill(_portalDarkness);
    }
}
