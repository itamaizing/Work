using Gangdollarff;
using UnityEngine;

public class BlockingClapOfLightTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<ClapOfLight>().EnableBlockingClapTalent(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<ClapOfLight>().EnableBlockingClapTalent(false);
    }
}
