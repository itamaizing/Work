using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvolutionTalent_3 : Talent
{
    [SerializeField] private CheliceraStrike cheliceraStrike;
    [SerializeField] private ClawStrike clawStrike;
    [SerializeField] private JumpBack jumpBack;
    [SerializeField] private SkillManager skillManager;

    public override void Enter()
    {
        skillManager.ActivateSkill(jumpBack);
        cheliceraStrike.CheliceraStrikeSpeed(true);
        clawStrike.ClawStrikeSpeed(true);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(jumpBack);
        cheliceraStrike.CheliceraStrikeSpeed(false);
        clawStrike.ClawStrikeSpeed(false);
    }
}
