using UnityEngine;

public class ComboPointTransferTalent : Talent
{
    public override void Enter()
    {
        Skill skill = character.Abilities.GetSkill<ComboPointTransferSkill>();
        character.Abilities.ActivateSkill(skill);
    }

    public override void Exit()
    {
        Skill skill = character.Abilities.GetSkill<ComboPointTransferSkill>();
        character.Abilities.DeactivateSkill(skill);
    }
}
