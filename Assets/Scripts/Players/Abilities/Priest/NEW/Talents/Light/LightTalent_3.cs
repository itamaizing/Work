using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightTalent_3 : Talent
{
    [SerializeField] StunMagicPassiveSkill priestPassiveSkill;
    [SerializeField] SkillManager manager;

    public override void Enter()
    {
        manager.ActivateSkill(priestPassiveSkill);
        priestPassiveSkill.DamageDarkLightStun(true);
    }

    public override void Exit()
    {
        manager.DeactivateSkill(priestPassiveSkill);
        priestPassiveSkill.DamageDarkLightStun(false);
    }
}
