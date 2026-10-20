using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisciplineTalent_7 : Talent
{
    public override void Enter()
    {
        character.Abilities.ActivateSkill(character.Abilities.GetSkill<DispelMagic>());
    }

    public override void Exit()
    {
        character.Abilities.DeactivateSkill(character.Abilities.GetSkill<DispelMagic>());
    }
}
