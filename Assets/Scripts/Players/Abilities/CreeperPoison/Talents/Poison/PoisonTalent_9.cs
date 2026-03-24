using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonTalent_9 : Talent
{
    [SerializeField] private PoisonBall _poisonBall;

    public override void Enter()
    {
        _poisonBall.HealingPoisonBall(true);
    }

    public override void Exit()
    {
        _poisonBall.HealingPoisonBall(false);
    }
}
