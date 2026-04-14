using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReptileTalent_5 : Talent
{
    [SerializeField] private CreeperPoisonAura _creeperPoisonAura; 

    public override void Enter()
    {
        _creeperPoisonAura.OwnElement(true);
    }

    public override void Exit()
    {
        _creeperPoisonAura.OwnElement(false);
    }
}

