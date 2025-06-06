using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SilenceTalenCooldown : Talent
{
	[SerializeField] private Silence silence;

    public override void Enter()
    {
        if (silence != null)
        {
            silence.SetReducedCooldown(true);
        }
    }

    public override void Exit()
    {
        if (silence != null)
        {
            silence.SetReducedCooldown(false);
        }
    }
}
