using UnityEngine;

public class DisciplineTalent_11 : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<PriestShield>().EnableBooster(PriestShield.PriestShieldBoosterType.LightShieldManaRestore, true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<PriestShield>().EnableBooster(PriestShield.PriestShieldBoosterType.LightShieldManaRestore, false);
    }
}
