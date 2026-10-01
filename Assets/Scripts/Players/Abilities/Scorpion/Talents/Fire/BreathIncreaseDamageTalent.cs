using UnityEngine;

public class BreathIncreaseDamageTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<FireBreath_Scorpion>().SetIncreasedExposuredDamage(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<FireBreath_Scorpion>().SetIncreasedExposuredDamage(false);
    }
}
