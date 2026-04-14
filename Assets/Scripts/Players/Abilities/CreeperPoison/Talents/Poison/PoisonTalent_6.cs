using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonTalent_6 : Talent
{
    [SerializeField] private SneakySpit _sneakySpit;
    [SerializeField] private SpitPoison _spitPoison;

    public override void Enter()
    {
        _sneakySpit.ErodedArmorState(true);
    }

    public override void Exit()
    {
        _sneakySpit.ErodedArmorState(false);
    }
}
