using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NatureTalent_9 : Talent
{
    [SerializeField] private TerrifyingElfAura _terrifyingElfAura;

    public override void Enter()
    {
        _terrifyingElfAura.CalmnessAura(true);
    }

    public override void Exit()
    {
        _terrifyingElfAura.CalmnessAura(false);
    }
}
