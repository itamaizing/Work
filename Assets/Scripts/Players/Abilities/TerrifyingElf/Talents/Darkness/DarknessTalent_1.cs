using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarknessTalent_1 : Talent
{
    [SerializeField] private Ghost ghost;
    [SerializeField] private PullingHealth pullingHealth;
    [SerializeField] private SkillManager ability;

    public override void Enter()
    {
        ability.ActivateSkill(pullingHealth);
        ability.ActivateSkill(ghost);
        ghost.CooldownGhostShotActiveTalent(true);
        pullingHealth.PullingHealthSpeedWithFearTalentActive(true);
    }

    public override void Exit()
    {
        ability.DeactivateSkill(pullingHealth);
        ability.DeactivateSkill(ghost);
        ghost.CooldownGhostShotActiveTalent(false);
        pullingHealth.PullingHealthSpeedWithFearTalentActive(false);
    }
}
