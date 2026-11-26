using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathTalent_3 : Talent
{
    [SerializeField] private NinjaResources ninjaResources;

    public override void Enter()
    {
        ninjaResources.HardenedFleshTalent(true);
    }

    public override void Exit()
    {
        ninjaResources.HardenedFleshTalent(false);
    }
}
