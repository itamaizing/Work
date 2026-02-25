using System;
using UnityEngine;

public class PsionicsTalent_11 : Talent
{
    [SerializeField] private AttackingPsionicEnergy _attackingPsionicEnergy;

    public override void Enter()
    {
        _attackingPsionicEnergy.AttackingPsiIncrease(true);
    }

    public override void Exit()
    {
        _attackingPsionicEnergy.AttackingPsiIncrease(false);
    }
}
