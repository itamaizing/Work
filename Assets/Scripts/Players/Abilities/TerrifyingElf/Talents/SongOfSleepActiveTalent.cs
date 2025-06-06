using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SongOfSleepTalent : Talent
{
    [SerializeField] private SongOfSleep songOfSleep;
    [SerializeField] private SkillManager _ability;

    public override void Enter()
    {
        _ability.ActivateSkill(songOfSleep);
    }

    public override void Exit()
    {
        _ability.DeactivateSkill(songOfSleep);
    }
}
