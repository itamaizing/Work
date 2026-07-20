using UnityEngine;

public class NatureTalent_12 : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<TerrifyingElfAura>().EnableCooldownReduceTalent(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<TerrifyingElfAura>().EnableCooldownReduceTalent(false);
    }
}
