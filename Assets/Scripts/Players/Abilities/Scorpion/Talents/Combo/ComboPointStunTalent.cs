using UnityEngine;

public class ComboPointStunTalent : Talent
{
    public override void Enter()
    {
        var consumeSkill = character.Abilities.GetSkill<ConsumeCombo_Scorpion>();
        if (consumeSkill != null)
            consumeSkill.SetCanCastUnderPhysicalDisable(true);
    }

    public override void Exit()
    {
        var consumeSkill = character.Abilities.GetSkill<ConsumeCombo_Scorpion>();
        if (consumeSkill != null)
            consumeSkill.SetCanCastUnderPhysicalDisable(false);
    }
}