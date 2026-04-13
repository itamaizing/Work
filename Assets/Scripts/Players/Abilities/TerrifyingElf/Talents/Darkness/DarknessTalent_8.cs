using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarknessTalent_8 : Talent
{
    [SerializeField] private TerrifyingElfAura terrifyingElfAura;
    [SerializeField] private Ghost ghost;
    [SerializeField] private Silence silence;

    public override void Enter()
    {
        ghost.PullingHealthGostTeleport(true);
        terrifyingElfAura.ReductionRecharge(true);
        silence.WeakeningSilenceTalentActive(true);
    }

    public override void Exit()
    {
        ghost.PullingHealthGostTeleport(false);
        terrifyingElfAura.ReductionRecharge(false);
        silence.WeakeningSilenceTalentActive(true);
    }
}
