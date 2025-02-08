using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PullingHealthGhostTalent : Talent
{
    [SerializeField] private PullingHealth pullingHealth;

    public override void Enter()
    {
        pullingHealth.SetPullingHealthGhostTalentActive(true);
    }

    public override void Exit()
    {
        pullingHealth.SetPullingHealthGhostTalentActive(false);
    }
}
