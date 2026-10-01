using UnityEngine;

public class EvolutionTalent_10 : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<ClawStrike>().EvolutionTalentTen(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<ClawStrike>().EvolutionTalentTen(true);
    }
}
