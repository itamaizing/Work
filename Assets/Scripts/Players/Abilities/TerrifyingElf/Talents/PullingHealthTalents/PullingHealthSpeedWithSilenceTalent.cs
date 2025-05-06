using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PullingHealthSpeedWithSilenceTalent : Talent
{
    [SerializeField] private PullingHealth pullingHealth;

    public override void Enter()
    {
        pullingHealth.PullingHealthSpeedWithSilenceTalentActive(true);
    }

    public override void Exit()
    {
        pullingHealth.PullingHealthSpeedWithSilenceTalentActive(false);
    }
}
