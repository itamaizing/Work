using UnityEngine;

public class DisciplineTalent_8 : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<SoulAid>()?.TiredSoulBooster?.Enable(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<SoulAid>()?.TiredSoulBooster?.Enable(false);
    }
}
