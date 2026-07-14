using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaTalent_7 : Talent
{
    [SerializeField] private IceRolling _iceRolling;

    public override void Enter()
    {
        Debug.LogError("miss AttackWithFrosenAddEvade");
        //_iceRolling.AttackWithFrosenAddEvade(true);
    }

    public override void Exit()
    {
        Debug.LogError("miss AttackWithFrosenAddEvade");
        //_iceRolling.AttackWithFrosenAddEvade(false);
    }
}
