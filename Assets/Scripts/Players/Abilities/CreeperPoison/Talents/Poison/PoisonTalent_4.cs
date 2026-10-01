using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonTalent_4 : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<PoisonBall>().SetPoisonCloudEnabled(true);
        character.Abilities.GetSkill<PoisonSlap>().SetPoisonCloudEnabled(true);
        character.Abilities.GetSkill<SpitPoison>().SetPoisonCloudEnabled(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<PoisonBall>().SetPoisonCloudEnabled(false);
        character.Abilities.GetSkill<PoisonSlap>().SetPoisonCloudEnabled(false);
        character.Abilities.GetSkill<SpitPoison>().SetPoisonCloudEnabled(false);
    }
}
