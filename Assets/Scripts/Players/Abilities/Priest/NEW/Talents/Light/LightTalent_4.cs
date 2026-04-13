using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightTalent_4 : Talent
{
    public override void Enter()
    {
        var priestShield = character.Abilities.GetSkill<PriestShield>();
        if (priestShield != null)
            priestShield.HealingBoostBooster.Enable(true);
    }

    public override void Exit()
    {
        var priestShield = character.Abilities.GetSkill<PriestShield>();
        if (priestShield != null)
        {
            priestShield.HealingBoostBooster.Enable(false);
            priestShield.HealingBoostBooster.ResetAccumulator();
        }
    }
}
