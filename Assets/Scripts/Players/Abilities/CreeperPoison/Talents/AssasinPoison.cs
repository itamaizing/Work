using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssasinPoison : Talent
{
    [SerializeField] private CreeperInvisible _invisibleCreeper;
    [SerializeField] private CreeperStrike _creeperStrike;

    public override void Enter()
    {
        isActive = true;
    }

    public override void Exit()
    {
        isActive = false;
    }


}
