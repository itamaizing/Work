using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonTalent_1 : Talent
{
    public override void Enter()
    {
        character.Abilities.ActivateSkill(character.Abilities.GetSkill<SpitPoison>());
    }

    public override void Exit()
    {
        character.Abilities.DeactivateSkill(character.Abilities.GetSkill<SpitPoison>());
    }
}
