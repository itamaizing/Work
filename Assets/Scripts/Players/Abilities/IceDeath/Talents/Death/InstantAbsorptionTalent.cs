using UnityEngine;

public class InstantAbsorptionTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<IceDeathAbsorbation>().EnableInstantAbsorption(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<IceDeathAbsorbation>().EnableInstantAbsorption(false);
    }
}
