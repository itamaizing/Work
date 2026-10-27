using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaPoisonTalent_10 : Talent
{
    [SerializeField] private PoisonSlap _poisonSlap;

    public override void Enter()
    {
        character.Abilities.GetSkill<ColdBlood>().SetPoisonEmpowerActive(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<ColdBlood>().SetPoisonEmpowerActive(false);
    }
}