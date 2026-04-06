using UnityEngine;

public class D5TalentShield : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<PriestShield>()?.EnableBooster(PriestShield.PriestShieldBoosterType.PhysicalShieldBoost, true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<PriestShield>()?.EnableBooster(PriestShield.PriestShieldBoosterType.PhysicalShieldBoost, false);
    }
}
