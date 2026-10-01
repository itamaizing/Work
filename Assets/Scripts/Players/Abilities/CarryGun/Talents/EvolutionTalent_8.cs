using UnityEngine;

public class EvolutionTalent_8 : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<CheliceraStrike>().ChanceCritDamageIncrease(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<CheliceraStrike>().ChanceCritDamageIncrease(false);
    }
}
