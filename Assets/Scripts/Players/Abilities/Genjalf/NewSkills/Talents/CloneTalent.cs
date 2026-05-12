using UnityEngine;

public class CloneTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.ActivateSkill(character.Abilities.GetSkill<CloneSelfSkill>());
    }

    public override void Exit()
    {
        character.Abilities.DeactivateSkill(character.Abilities.GetSkill<CloneSelfSkill>());
    }
}
