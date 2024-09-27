using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealingPoisonBall : Talent
{
    private void Start()
    {
        Enter();
    }

    public override void Enter()
    {
        SetActive(true);
    }

    public override void Exit()
    {
        SetActive(false);
    }
}
