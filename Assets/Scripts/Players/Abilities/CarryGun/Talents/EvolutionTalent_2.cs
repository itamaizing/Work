using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvolutionTalent_2 : Talent
{
    [SerializeField] private CheliceraStrike cheliceraStrike;
    [SerializeField] private JumpWithChelicera jumpWithChelicera;
    [SerializeField] private ClawStrike clawStrike;
    [SerializeField] private SkillManager skillManager;

    public override void Enter()
    {
        clawStrike.BleedingClawStrike(true);
        cheliceraStrike.EvolutionTalentTwo(true);
        skillManager.ActivateSkill(jumpWithChelicera);
    }

    public override void Exit()
    {
        clawStrike.BleedingClawStrike(false);
        cheliceraStrike.EvolutionTalentTwo(false);
        skillManager.DeactivateSkill(jumpWithChelicera);
    }
}
