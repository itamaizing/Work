using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwarmTalent_3 : Talent
{
    [SerializeField] private Tentacles tentacles;
    [SerializeField] private InjectionAdrenaline _injectionAdrenaline;

    [SerializeField] private SkillManager _skillManager;

    public override void Enter()
    {
        _skillManager.ActivateSkill(_injectionAdrenaline);
        tentacles.AttractionTentacleTalent(true);
        AddingDescriptionSet(true);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_injectionAdrenaline);
        tentacles.AttractionTentacleTalent(false);
        AddingDescriptionSet(false);
    }

    private void AddingDescriptionSet(bool value)
    {
        tentacles.AddingDescriptionSet(value, Data.DescriptionsForInfoPanel[0]);
    }
}
