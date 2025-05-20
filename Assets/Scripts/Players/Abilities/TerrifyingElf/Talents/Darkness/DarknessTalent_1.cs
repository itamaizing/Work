using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarknessTalent_1 : Talent
{
    [SerializeField] private Ghost ghost;
    [SerializeField] private PullingHealth pullingHealth;

    public override void Enter()
    {
        ghost.CooldownGhostShotActiveTalent(true);
        pullingHealth.PullingHealthSpeedWithSilenceTalentActive(true);
    }

    public override void Exit()
    {
        ghost.CooldownGhostShotActiveTalent(false);
        pullingHealth.PullingHealthSpeedWithSilenceTalentActive(true);
    }
}
