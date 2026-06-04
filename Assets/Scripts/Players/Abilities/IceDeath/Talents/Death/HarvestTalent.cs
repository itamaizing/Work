using UnityEngine;

public class HarvestTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<HarvestOfRunes>().SetHarvestTalent(true);
        character.Abilities.GetSkill<HarvestOfEnergy>().SetHarvestTalent(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<HarvestOfRunes>().SetHarvestTalent(false);
        character.Abilities.GetSkill<HarvestOfEnergy>().SetHarvestTalent(false);
    }
}
