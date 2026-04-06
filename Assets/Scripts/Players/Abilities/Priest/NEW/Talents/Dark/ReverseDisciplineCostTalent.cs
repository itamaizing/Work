using UnityEngine;

public class ReverseDisciplineCostTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<ReversePolarity>()?.ReverseDisciplineBooster.Enable(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<ReversePolarity>()?.ReverseDisciplineBooster.Enable(false);
    }
}
