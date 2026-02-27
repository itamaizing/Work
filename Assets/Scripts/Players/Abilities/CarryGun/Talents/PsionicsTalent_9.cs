using System;
using UnityEngine;

public class PsionicsTalent_9 : Talent
{
    [SerializeField] private Impatica impatica;

    public override void Enter()
    {
        impatica.ExtendDamageAbsorption(true);
    }

    public override void Exit()
    {
        impatica.ExtendDamageAbsorption(false);
    }
}
