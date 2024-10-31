using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CapaciousPoisonCloud : Talent
{
    public override void Enter()
    {
        SetActive(true);
    }

    public override void Exit()
    {
        SetActive(false);
    }
}
