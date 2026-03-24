using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonTalent_9 : Talent
{
    [SerializeField] private PoisonBall _poisonBall;
    [SerializeField] private SpitPoison _spitPoison;

    public override void Enter()
    {
        _poisonBall.HealingPoisonBall(true);
        _spitPoison.ActiveHealingSpitPoison(true);
    }

    public override void Exit()
    {
        _poisonBall.HealingPoisonBall(false);
        _spitPoison.ActiveHealingSpitPoison(false);
    }
}
