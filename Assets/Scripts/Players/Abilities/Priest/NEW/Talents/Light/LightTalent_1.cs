using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightTalent_1 : Talent
{
    [SerializeField] private FlashOfLight flashOfLight;
    [SerializeField] private SkillManager _manager;

    //FOR TEST ONLY
    [SerializeField] StunMagicPassiveSkill stunMagicPassiveSkill;
    [SerializeField] Dark1PassiveSkill _darkPassiveSkill;

    public override void Enter()
    {

        _manager.ActivateSkill(flashOfLight);

        //TEST ONLY!!!
        if (Data.Level >= 2)
        {
            _manager.ActivateSkill(stunMagicPassiveSkill);
            stunMagicPassiveSkill.DamageDarkLightStun(true, Data.DescriptionsForInfoPanel[0]);
        }
        if (Data.Level >= 3)
        {
            _manager.ActivateSkill(_darkPassiveSkill);
        }
    }

    public override void Exit()
    {
        _manager.DeactivateSkill(flashOfLight);

        //TEST ONLY!!!
        _manager.DeactivateSkill(stunMagicPassiveSkill);
        _manager.DeactivateSkill(_darkPassiveSkill);

    }
}
