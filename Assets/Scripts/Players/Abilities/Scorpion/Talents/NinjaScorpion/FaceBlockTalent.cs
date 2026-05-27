using UnityEngine;

public class FaceBlockTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.ActivateSkill(character.Abilities.GetSkill<FaceBlock_Scorpion>());
    }

    public override void Exit()
    {
        character.Abilities.DeactivateSkill(character.Abilities.GetSkill<FaceBlock_Scorpion>());
    }
}
