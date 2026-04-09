using UnityEngine;

public class MagicalComboTalent : Talent
{
    public override void Enter()
    {
        var passiveCombo = character.GetComponent<PassiveCombo_Scorpion>();
        if (passiveCombo != null)
            passiveCombo.ImpulseFireBooster.Enable(true);
    }

    public override void Exit()
    {
        var passiveCombo = character.GetComponent<PassiveCombo_Scorpion>();
        if (passiveCombo != null)
            passiveCombo.ImpulseFireBooster.Enable(false);
    }
}
