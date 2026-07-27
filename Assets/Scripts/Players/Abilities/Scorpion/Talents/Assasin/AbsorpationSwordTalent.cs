using UnityEngine;

public class AbsorpationSwordTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.ActivateSkill(character.Abilities.GetSkill<AbsorbationSwordSkill>());
    }

    public override void Exit()
    {
        character.Abilities.DeactivateSkill(character.Abilities.GetSkill<AbsorbationSwordSkill>());
    }
}
