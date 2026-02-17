using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwarmTalent_7 : Talent
{
    [SerializeField] private Tentacles _tentacles;

    public override void Enter()
    {
        _tentacles.ProtectiveCooconSpawn(true);
    }

    public override void Exit()
    {
        _tentacles.ProtectiveCooconSpawn(false);
    }
}
