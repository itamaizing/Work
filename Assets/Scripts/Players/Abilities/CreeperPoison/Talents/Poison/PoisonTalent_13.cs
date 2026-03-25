using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonTalent_13 : Talent
{
    [SerializeField] private WaveParalyzingPoison _waveParalyzingPoison;
    [SerializeField] private SkillManager _skillManager;

    public override void Enter()
    {
        _skillManager.ActivateSkill(_waveParalyzingPoison);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_waveParalyzingPoison);
    }
}
