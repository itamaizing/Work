using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarknessTalent_9 : Talent
{
    [SerializeField] private RetributiveReckoning _retributiveReckoning;

    public override void Enter()
    {
        _retributiveReckoning.MagicAbilityInstantly(true);
    }

    public override void Exit()
    {
        _retributiveReckoning.MagicAbilityInstantly(false);
    }
}
