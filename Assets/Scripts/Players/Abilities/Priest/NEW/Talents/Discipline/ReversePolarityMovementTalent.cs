using UnityEngine;

public class ReversePolarityMovementTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<ReversePolarity>()?.ReversePolarityMovementBooster?.Enable(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<ReversePolarity>()?.ReversePolarityMovementBooster?.Enable(false);
    }
}
