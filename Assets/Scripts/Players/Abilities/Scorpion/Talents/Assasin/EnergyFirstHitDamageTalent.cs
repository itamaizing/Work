using UnityEngine;

public class EnergyFirstHitDamageTalent : Talent
{
    private EnergyFirstHitDamageBooster _booster;
    public override void Enter()
    {
        character.Abilities.GetSkill<EnergyFirstHitDamageBooster>().EnableBooster(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<EnergyFirstHitDamageBooster>().EnableBooster(false);
    }
}