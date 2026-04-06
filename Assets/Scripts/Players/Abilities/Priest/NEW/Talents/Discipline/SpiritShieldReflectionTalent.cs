using UnityEngine;

public class SpiritShieldReflectionTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<PriestShield>()?.EnableReflectionBooster(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<PriestShield>()?.EnableReflectionBooster(false);
    }
}
