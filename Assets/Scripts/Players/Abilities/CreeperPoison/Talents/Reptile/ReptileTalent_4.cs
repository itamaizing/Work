using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ReptileTalent_4 : Talent
{
    [SerializeField] private SpeedOfReptile speed;
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private MetabolismReptile _metabolismReptile;

    public override void Enter()
    {
        skillManager.ActivateSkill(speed);
        skillManager.ActivateSkill(_metabolismReptile);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(speed);
        skillManager.DeactivateSkill(_metabolismReptile);
    }
}

