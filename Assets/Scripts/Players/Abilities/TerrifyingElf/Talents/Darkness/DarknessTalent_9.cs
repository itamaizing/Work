using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarknessTalent_9 : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<Ghost>().PassingThroughGhost(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<Ghost>().PassingThroughGhost(false);
    }
}
