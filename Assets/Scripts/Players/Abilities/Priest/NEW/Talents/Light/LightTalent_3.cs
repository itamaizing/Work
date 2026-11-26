using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightTalent_3 : Talent
{
    [SerializeField] StunMagicPassiveSkill stunMagicPassiveSkill;
    [SerializeField] SkillManager manager;

    public override void Enter()
    {
        manager.ActivateSkill(stunMagicPassiveSkill);
        stunMagicPassiveSkill.DamageDarkLightStun(true);
    }

    public override void Exit()
    {
        manager.DeactivateSkill(stunMagicPassiveSkill);
        stunMagicPassiveSkill.DamageDarkLightStun(false);
    }


}
