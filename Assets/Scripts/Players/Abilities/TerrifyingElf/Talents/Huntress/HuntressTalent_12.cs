using UnityEngine;

public class HuntressTalent_12 : Talent
{

    public override void Enter()
    {
        character.Abilities.GetSkill<ReconnaissanceFire>().partialBlindnessTalentActive(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<ReconnaissanceFire>().partialBlindnessTalentActive(false);
    }
}
