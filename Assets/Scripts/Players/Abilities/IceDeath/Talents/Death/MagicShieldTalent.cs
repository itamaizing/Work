using UnityEngine;

public class MagicShieldTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.ActivateSkill(character.Abilities.GetSkill<MagicDefenceSkill>());
    }

    public override void Exit()
    {
        character.Abilities.DeactivateSkill(character.Abilities.GetSkill<MagicDefenceSkill>());
    }
}
