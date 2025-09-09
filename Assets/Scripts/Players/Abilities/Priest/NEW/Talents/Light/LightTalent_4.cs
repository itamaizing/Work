using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightTalent_4 : Talent
{
    [SerializeField] private PriestShield _priestShield;

    public override void Enter()
    {
        _priestShield.EnableHealingBoost(true);
    }

    public override void Exit()
    {
        _priestShield.EnableHealingBoost(false);
    }
}
