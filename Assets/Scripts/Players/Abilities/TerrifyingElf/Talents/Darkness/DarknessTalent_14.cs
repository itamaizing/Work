using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarknessTalent_14 : Talent
{
    [SerializeField] private Ghost ghost;
    [SerializeField] private PullingHealth pullingHealth;
    [SerializeField] private Silence silence;

    //#ПЕРЕНЕСЕНО В 8М
    public override void Enter()
    {
        /*ghost.CooldownGhostShotActiveTalent(true);
        pullingHealth.SetPullingHealthGhostTalentActive(true);
        silence.SilenceEffectsOnMinionMagic(true);
        silence.GhostDeathSilence(true);
        silence.SilenceEffectGhostCast(true);
        ghost.PullingHealthGostTeleport(true);*/
    }

    public override void Exit()
    {
        /*ghost.CooldownGhostShotActiveTalent(false);
        pullingHealth.SetPullingHealthGhostTalentActive(false);
        silence.SilenceEffectsOnMinionMagic(false);
        silence.GhostDeathSilence(false);
        silence.SilenceEffectGhostCast(false);
        ghost.PullingHealthGostTeleport(false);*/
    }
}
