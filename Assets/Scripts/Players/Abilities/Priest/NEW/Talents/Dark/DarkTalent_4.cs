using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarkTalent_4 : Talent
{
    public override void Enter()
    {
        var priestShield = character.Abilities.GetSkill<PriestShield>();
        priestShield?.DarkMagicBoostBooster.Enable(true);
    }

    public override void Exit()
    {
        var priestShield = character.Abilities.GetSkill<PriestShield>();
        if (priestShield != null)
        {
            priestShield.DarkMagicBoostBooster.Enable(false);
            priestShield.DarkMagicBoostBooster.ResetAccumulator();
        }
    }
}
