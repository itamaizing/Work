using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarknessTalent_10 : Talent
{

    public override void Enter()
    {
        character.Abilities.ActivateSkill(character.Abilities.GetSkill<Suppression>());
    }

    public override void Exit()
    {
        character.Abilities.DeactivateSkill(character.Abilities.GetSkill<Suppression>());
    }
}
