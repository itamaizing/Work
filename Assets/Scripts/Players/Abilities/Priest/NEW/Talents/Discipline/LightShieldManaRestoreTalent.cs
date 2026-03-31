using UnityEngine;

public class LightShieldManaRestoreTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<PriestShield>()?.EnableBooster(PriestShield.PriestBoosterType.LightShieldManaRestore,true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<PriestShield>()?.EnableBooster(PriestShield.PriestBoosterType.LightShieldManaRestore,false);
    }
}
