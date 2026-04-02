using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReptileTalent_7 : Talent
{
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private CreeperStrike _creeperStrike;
    [SerializeField] private SpeedOfReptile speed;

    public override void Enter()
    {
        _creeperStrike.SetReptileTalentActive(true);
        skillManager.ActivateSkill(speed);
    }

    public override void Exit()
    {
        _creeperStrike.SetReptileTalentActive(false);
        skillManager.DeactivateSkill(speed);
    }
}

