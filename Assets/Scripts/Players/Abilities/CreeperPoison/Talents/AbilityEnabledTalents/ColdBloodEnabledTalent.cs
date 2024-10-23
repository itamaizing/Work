using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ColdBloodEnabledTalent : Talent
{
    [SerializeField] private ColdBlood _coldBlood;
    [SerializeField] private SkillManager _skillManager;

    public override void Enter()
    {
        SetActive(true);
        if (!_skillManager.Abilities.Contains(_coldBlood))
        {
            _skillManager.ActivateSkill(_coldBlood);
        }
    }

    public override void Exit()
    {
        SetActive(false);
        if (_skillManager.Abilities.Contains(_coldBlood))
        {
            _skillManager.DeactivateSkill(_coldBlood);
        }
    }
}
