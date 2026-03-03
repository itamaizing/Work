
public class GodAuraTalent : Talent
{
    public override void Enter()
    {
        character.CharacterState.CmdAddState(States.GodAura,100,0,character.gameObject,nameof(GodAura));
    }

    public override void Exit()
    {
        character.CharacterState.CmdRemoveState(States.GodAura);
    }
}
