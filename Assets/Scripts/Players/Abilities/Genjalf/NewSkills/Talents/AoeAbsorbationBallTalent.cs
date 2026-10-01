using Gangdollarff;
using UnityEngine;

public class AoeAbsorbationBallTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<AbsorptionBall>().EnableAoeShieldTalent(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<AbsorptionBall>().EnableAoeShieldTalent(false);
    }
}
