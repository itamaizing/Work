using UnityEngine;

public class ComboMaxStackTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<ConsumeCombo_Scorpion>().OnComboStacksIncreased(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<ConsumeCombo_Scorpion>().OnComboStacksIncreased(false);
    }
}