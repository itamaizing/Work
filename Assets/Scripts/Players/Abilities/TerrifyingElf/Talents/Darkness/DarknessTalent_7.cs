using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarknessTalent_7 : Talent
{
    [SerializeField] private SubjugationMind subjugationMind;
    [SerializeField] private SongOfSleep songOfSleep;
    [SerializeField] private SkillManager ability;
    [SerializeField] private Ghost ghost;

    public override void Enter()
    {
        ghost.PassingThroughGhost(true);
        songOfSleep.SleepInnerDarknessTalent(true);
        ability.ActivateSkill(subjugationMind);
    }

    public override void Exit()
    {
        ghost.PassingThroughGhost(false);
        songOfSleep.SleepInnerDarknessTalent(false);
        ability.DeactivateSkill(subjugationMind);
    }
}
