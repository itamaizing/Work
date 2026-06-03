using UnityEngine;

public class SpreadIgnitionTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<IgnitionSkill>().EnableIgnitionSpreadTalent(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<IgnitionSkill>().EnableIgnitionSpreadTalent(false);
    }
}
