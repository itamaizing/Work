using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwarmTalent_8 : Talent
{
    [SerializeField] private SwarmCapacity _swarmCapacity;

    public override void Enter()
    {
        _swarmCapacity.BoostSpeedSwarmDamage(true);
    }

    public override void Exit()
    {
        _swarmCapacity.BoostSpeedSwarmDamage(false);
    }
}
