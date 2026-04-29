using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrostTalent_11 : Talent
{
    [SerializeField] private NinjaResources _ninjaResources;

    public override void Enter()
    {
        _ninjaResources.RuneRegenSpeed(true);
    }

    public override void Exit()
    {
        _ninjaResources.RuneRegenSpeed(false);
    }
}
