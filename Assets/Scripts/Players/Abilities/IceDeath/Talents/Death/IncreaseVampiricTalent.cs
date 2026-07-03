using UnityEngine;

public class IncreaseVampiricTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<NinjaResources>().EnableIncreasedVampiric(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<NinjaResources>().EnableIncreasedVampiric(false);
    }
}
