using System;
using UnityEngine;

public class PsionicsTalent_10 : Talent
{
    [SerializeField] private Impatica _impatica;

    public override void Enter()
    {
        _impatica.SecondCharge(true);
    }

    public override void Exit()
    {
        _impatica.SecondCharge(false);
    }
}
