using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvolutionTalent_1 : Talent
{
    [SerializeField] private CheliceraStrike cheliceraStrike;
    [SerializeField] private JumpWithChelicera jumpWithChelicera;
    [SerializeField] private SkillManager skillManager;

    public override void Enter()
    {
        skillManager.ActivateSkill(cheliceraStrike);
        jumpWithChelicera.EvolutionTalentOne(true);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(cheliceraStrike);
        jumpWithChelicera.EvolutionTalentOne(false);
    }
}
