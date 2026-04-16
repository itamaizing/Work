using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaTalent_9 : Talent
{
    [SerializeField] private IceSword _iceSword;

    public override void Enter()
    {
        _iceSword.FrozenCrit(true);
    }

    public override void Exit()
    {
        _iceSword.FrozenCrit(false);
    }
}
