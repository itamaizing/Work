using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonTalent_7 : Talent
{
    [SerializeField] private PoisonBall _poisonBall;

    public override void Enter()
    {
        _poisonBall.PoisonCloudAddPoisonBone(true);
    }

    public override void Exit()
    {
        _poisonBall.PoisonCloudAddPoisonBone(false);
    }
}
