using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarknessTalent_14 : Talent
{
    [SerializeField] private Ghost ghost;
    [SerializeField] private PullingHealth pullingHealth;
    [SerializeField] private Silence silence;

    public override void Enter()
    {
        ghost.CooldownGhostShotActiveTalent(true);
        pullingHealth.SetPullingHealthGhostTalentActive(true);
        silence.SilenceEffectsOnMinionMagic(true);
        silence.GhostDeathSilence(true);
    }

    public override void Exit()
    {
        ghost.CooldownGhostShotActiveTalent(false);
        pullingHealth.SetPullingHealthGhostTalentActive(false);
        silence.SilenceEffectsOnMinionMagic(false);
        silence.GhostDeathSilence(false);
    }
}
