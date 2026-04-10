using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonTalent_10 : Talent
{
    [SerializeField] ExplosionPoisonCloud _explosionCloud;

    public override void Enter()
    {
        _explosionCloud.RestorativePoison(true);
    }

    public override void Exit()
    {
        _explosionCloud.RestorativePoison(false);
    }
}
