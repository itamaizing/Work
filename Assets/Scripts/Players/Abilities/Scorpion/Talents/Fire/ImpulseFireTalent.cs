
public class ImpulseFireTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<Teleportation_Scorpion>().ImpulseFireBooster?.Enable(true);
        
        var passiveCombo = character.GetComponent<PassiveCombo_Scorpion>();
        if (passiveCombo != null)
            passiveCombo.ImpulseFireBooster.Enable(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<Teleportation_Scorpion>().ImpulseFireBooster?.Enable(false);

        var passiveCombo = character.GetComponent<PassiveCombo_Scorpion>();
        if (passiveCombo != null)
            passiveCombo.ImpulseFireBooster.Enable(false);
    }
}