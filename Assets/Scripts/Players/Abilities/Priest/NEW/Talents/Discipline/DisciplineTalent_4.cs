using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisciplineTalent_4 : Talent
{
    public override void Enter()
    {
        character.Abilities.ActivateSkill(character.Abilities.GetSkill<SoulAid>());
    }

    public override void Exit()
    {
        character.Abilities.DeactivateSkill(character.Abilities.GetSkill<SoulAid>());
    }
}
