using UnityEngine;

public class EvolutionTalent_11 : Talent
{
    public override void Enter()
    {
        character.Abilities.ActivateSkill(character.Abilities.GetSkill<DoubleCheliceraStrike>());
    }

    public override void Exit()
    {
        character.Abilities.DeactivateSkill(character.Abilities.GetSkill<DoubleCheliceraStrike>());
    }
}
