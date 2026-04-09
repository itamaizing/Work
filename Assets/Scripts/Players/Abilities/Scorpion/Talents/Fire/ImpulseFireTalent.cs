
public class ImpulseFireTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<Teleportation_Scorpion>().ImpulseFireBooster?.Enable(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<Teleportation_Scorpion>().ImpulseFireBooster?.Enable(false);
    }
}