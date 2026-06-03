using UnityEngine;

public class HellTeleportTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.ActivateSkill(character.Abilities.GetSkill<HellTeleportSkill>());
    }

    public override void Exit()
    {
        character.Abilities.DeactivateSkill(character.Abilities.GetSkill<HellTeleportSkill>());
    }
}
