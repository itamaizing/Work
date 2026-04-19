using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaTalent_7 : Talent
{
    [SerializeField] private IceRolling _iceRolling;

    public override void Enter()
    {
        _iceRolling.AttackWithFrosenAddEvade(true);
    }

    public override void Exit()
    {
        _iceRolling.AttackWithFrosenAddEvade(false);
    }
}
