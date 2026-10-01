using UnityEngine;

public class ComboHealOnDispelTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<ConsumeCombo_Scorpion>()?.HealOnDispelBooster.Enable(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<ConsumeCombo_Scorpion>()?.HealOnDispelBooster.Enable(false);
    }
}
