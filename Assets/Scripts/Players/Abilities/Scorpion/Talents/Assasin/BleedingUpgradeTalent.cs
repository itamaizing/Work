using UnityEngine;

public class BleedingUpgradeTalent : Talent
{
    public override void Enter()
    {
        if (character.Abilities.GetSkill<UndercutSkill>().IsSkillActive)
        {
            character.Abilities.GetSkill<UndercutSkill>().EnableBleedingUpgrade(true);
        }
    }

    public override void Exit()
    {
        if (character.Abilities.GetSkill<UndercutSkill>().IsSkillActive)
        {
            character.Abilities.GetSkill<UndercutSkill>().EnableBleedingUpgrade(false);
        }
    }
}
