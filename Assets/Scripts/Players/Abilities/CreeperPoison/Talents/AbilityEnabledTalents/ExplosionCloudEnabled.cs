using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionCloudEnabled : Talent
{
    [SerializeField] ExplosionPoisonCloud _explosionCloud;
    [SerializeField] private SkillManager _skillManager;

    public override void Enter()
    {
        SetActive(true);
        if (!_skillManager.Abilities.Contains(_explosionCloud))
        {
            _skillManager.ActivateSkill(_explosionCloud);
        }
    }

    public override void Exit()
    {
        SetActive(false);
        if (_skillManager.Abilities.Contains(_explosionCloud))
        {
            _skillManager.DeactivateSkill(_explosionCloud);
        }
    }
}
