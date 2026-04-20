using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrostTalent_4 : Talent
{
    [SerializeField] private IcePuddle _icePuddle;
    [SerializeField] IceShadow _iceShadow;
    [SerializeField] private SkillManager _skillManager;


    public override void Enter()
    {
        _icePuddle.IceDeathInIcePudleTalentActive(true);
        _skillManager.ActivateSkill(_iceShadow);
        AddingDescriptionSet(true);
    }

    public override void Exit()
    {
        _icePuddle.IceDeathInIcePudleTalentActive(false);
        _skillManager.ActivateSkill(_iceShadow);
        AddingDescriptionSet(false);
    }

    private void AddingDescriptionSet(bool value)
    {
        _icePuddle.AddingDescriptionSet(value, Data.DescriptionsForInfoPanel[0]);
    }    
}
