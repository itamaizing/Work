using UnityEngine;

public class OverhealManaBoosterTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<FlowOfLight>().OverhealManaBooster.Enable(true);
        character.Abilities.GetSkill<FlashOfLight>().OverhealManaBooster.Enable(true);
        character.Abilities.GetSkill<SparkOfLight>().OverhealManaBooster.Enable(true);
        character.Abilities.GetSkill<RetributionLight>().OverhealManaBooster.Enable(true);
        character.Abilities.GetSkill<DomeOfLight>().OverhealManaBooster.Enable(true);
        character.Abilities.GetSkill<PillarOfLight>().OverhealManaBooster.Enable(true);
        character.Abilities.GetSkill<RadianceOfLight>().OverhealManaBooster.Enable(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<FlowOfLight>().OverhealManaBooster.Enable(false);
        character.Abilities.GetSkill<FlashOfLight>().OverhealManaBooster.Enable(false);
        character.Abilities.GetSkill<SparkOfLight>().OverhealManaBooster.Enable(false);
        character.Abilities.GetSkill<RetributionLight>().OverhealManaBooster.Enable(false);
        character.Abilities.GetSkill<DomeOfLight>().OverhealManaBooster.Enable(false);
        character.Abilities.GetSkill<PillarOfLight>().OverhealManaBooster.Enable(false);
        character.Abilities.GetSkill<RadianceOfLight>().OverhealManaBooster.Enable(false);
    }
}
