using UnityEngine;

public class ComboHealOnDispelTalent : Talent
{
    public override void Enter()
    {
        var consume = character.Abilities.GetSkill<ConsumeCombo_Scorpion>();
        if (consume != null)
            consume.SetHealOnDispelActive(true);
    }

    public override void Exit()
    {
        var consume = character.Abilities.GetSkill<ConsumeCombo_Scorpion>();
        if (consume != null)
            consume.SetHealOnDispelActive(false);
    }
}
