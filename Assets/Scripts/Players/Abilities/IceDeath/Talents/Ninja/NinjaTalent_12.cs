using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaTalent_12 : Talent
{
    [SerializeField] private NinjaResources _ninjaResources;

    public override void Enter()
    {
        _ninjaResources.FrozenCrit(true);
    }

    public override void Exit()
    {
        _ninjaResources.FrozenCrit(false);
    }
}
