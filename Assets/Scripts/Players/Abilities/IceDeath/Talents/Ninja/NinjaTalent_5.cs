using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaTalent_5 : Talent
{
    [SerializeField] private IceRolling _iceRolling;

    public override void Enter()
    {
        _iceRolling.DamageAddFrosting(true);
    }

    public override void Exit()
    {
        _iceRolling.DamageAddFrosting(false);
    }
}
