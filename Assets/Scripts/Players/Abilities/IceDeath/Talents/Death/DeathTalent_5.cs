

public class DeathTalent_5 : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<NinjaResources>().EnableVampiric(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<NinjaResources>().EnableVampiric(false);
    }
}
