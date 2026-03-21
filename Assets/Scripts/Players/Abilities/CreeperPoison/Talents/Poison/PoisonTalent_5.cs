using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonTalent_5 : Talent
{
    [SerializeField] private PoisonBall _poisonBall;

    public override void Enter()
    {
        _poisonBall.ActiveBallEffect(true);
    }

    public override void Exit()
    {
        _poisonBall.ActiveBallEffect(false);
    }
}
