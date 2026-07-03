using UnityEngine;

public class WarmingUpHealIncreaseTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<NewPunch_Scorpion>().WarmingUpHealingIncrease(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<NewPunch_Scorpion>().WarmingUpHealingIncrease(false);
    }
}
