using UnityEngine;

public class SwiftAttackBonusTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<SwiftAttacks_Scorpion>().ActivateSwiftBonus(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<SwiftAttacks_Scorpion>().ActivateSwiftBonus(false);
    }
}
