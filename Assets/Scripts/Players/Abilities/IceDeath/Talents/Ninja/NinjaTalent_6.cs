using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaTalent_6 : Talent
{
    [SerializeField] private IceRolling _iceRolling;

    public override void Enter()
    {
        Debug.LogError("miss DamageAddFrosting");
        //_iceRolling.DamageAddFrosting(true);
    }

    public override void Exit()
    {
        Debug.LogError("miss DamageAddFrosting");
        //_iceRolling.DamageAddFrosting(false);
    }
}
