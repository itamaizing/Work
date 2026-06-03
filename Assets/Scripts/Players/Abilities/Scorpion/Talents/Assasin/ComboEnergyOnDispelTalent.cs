using UnityEngine;

public class ComboEnergyOnDispelTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<ConsumeCombo_Scorpion>().EnergyOnDispelBooster?.Enable(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<ConsumeCombo_Scorpion>().EnergyOnDispelBooster?.Enable(false);
    }
}