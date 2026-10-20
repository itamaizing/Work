using UnityEngine;

public class DisciplineTalent_6 : Talent
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
