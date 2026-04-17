using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaTalent_7 : Talent
{
    [SerializeField] private IceShadow _iceShadow;
    [SerializeField] private IceRolling _iceRolling;

    public override void Enter()
    {
        _iceShadow.TalentEvade(true);
        _iceRolling.AttackWithFrosenAddEvade(true);
    }

    public override void Exit()
    {
        _iceShadow.TalentEvade(false);
        _iceRolling.AttackWithFrosenAddEvade(false);
    }
}
