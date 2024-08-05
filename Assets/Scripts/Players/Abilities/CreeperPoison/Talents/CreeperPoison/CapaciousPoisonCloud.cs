using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CapaciousPoisonCloud : Talent
{
    public override void Enter()
    {
        isActive = true;
    }

    public override void Exit()
    {
        isActive = false;
    }
}
