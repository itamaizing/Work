using UnityEngine;

public class HuntressTalent_1 : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<TerrifyingElfAura>().EnableHuntressTalent_1(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<TerrifyingElfAura>().EnableHuntressTalent_1(false);
    }
}
