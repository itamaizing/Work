using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarknessTalent_3 : Talent
{
    [SerializeField] private Silence silence;
    [SerializeField] private TerrifyingElfAura terrifyingElfAura;

    public override void Enter()
    {
        terrifyingElfAura.ReductionRecharge(true);
        silence.SetCanAttackMinions(true);
    }

    public override void Exit()
    {
        terrifyingElfAura.ReductionRecharge(false);
        silence.SetCanAttackMinions(false);
    }
}
