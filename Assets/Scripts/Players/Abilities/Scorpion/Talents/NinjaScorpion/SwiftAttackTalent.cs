using UnityEngine;

public class SwiftAttackTalent : Talent
{
    public override void Enter()
    {
        Skill skill = character.Abilities.GetSkill<SwiftAttacks_Scorpion>();
        character.Abilities.ActivateSkill(skill);
    }

    public override void Exit()
    {
        Skill skill = character.Abilities.GetSkill<SwiftAttacks_Scorpion>();
        character.Abilities.DeactivateSkill(skill);
    }
}