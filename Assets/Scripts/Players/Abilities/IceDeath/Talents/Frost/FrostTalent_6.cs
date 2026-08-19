public class FrostTalent_6 : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<IceShadow>().EndCastTalent(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<IceShadow>().EndCastTalent(false);
    }
}