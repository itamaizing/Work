using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarknessTalent_2 : Talent
{
    [SerializeField] private PullingHealth pullingHealth;
    [SerializeField] private Silence silence;

    public override void Enter()
    {
        pullingHealth.SetPullingHealthGhostTalentActive(true);
        silence.SilenceEffectsOnMinionMagic(true);
    }

    public override void Exit()
    {
        pullingHealth.SetPullingHealthGhostTalentActive(false);
        silence.SilenceEffectsOnMinionMagic(false);
    }
}
