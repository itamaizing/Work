using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbsoluteAccuracyTalent : Talent
{
    [SerializeField] private AbsoluteAccuracy _absoluteAccuracy;
    [SerializeField] private SkillManager _skillManager;

    private void Start()
    {
        Debug.Log("AbsoluteAccuracyTalent Started");
        Enter();
    }

    public override void Enter()
    {
        SetActive(true);
        if (_skillManager.Abilities.Contains(_absoluteAccuracy))
        {
            _skillManager.AddSkill(_absoluteAccuracy);
        }

    }

    public override void Exit()
    {
        SetActive(false);
        if (_skillManager.Abilities.Contains(_absoluteAccuracy))
        {
            _skillManager.RemoveSkill(_absoluteAccuracy);
        }
        else
        {
            _skillManager.RemoveSkill(_absoluteAccuracy);
        }
    }

}
