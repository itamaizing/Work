using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ColdBloodEnabledTalent : Talent
{
    [SerializeField] private ColdBlood _coldBlood;
    [SerializeField] private SkillManager _skillManager;

    private void Start()
    {
        Debug.Log("AbsoluteAccuracyTalent Started");
        Enter();
    }

    public override void Enter()
    {
        SetActive(true);
        if (_skillManager.Abilities.Contains(_coldBlood))
        {
            _skillManager.AddSkill(_coldBlood);
        }

    }

    public override void Exit()
    {
        SetActive(false);
        if (_skillManager.Abilities.Contains(_coldBlood))
        {
            _skillManager.RemoveSkill(_coldBlood);
        }
        else
        {
            _skillManager.RemoveSkill(_coldBlood);
        }
    }
}
