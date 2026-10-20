using UnityEngine;

public class DisciplineTalent_9 : Talent
{

    public override void Enter()
    {
        character.Abilities.GetSkill<PriestShield>().EnableBooster(PriestShield.PriestShieldBoosterType.SpiritShieldReflection, true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<PriestShield>().EnableBooster(PriestShield.PriestShieldBoosterType.SpiritShieldReflection, false);
    }
}
