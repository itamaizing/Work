using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarknessTalent_6 : Talent
{
    [SerializeField] private Suppression suppression;
    [SerializeField] private SkillManager ability;
    [SerializeField] private ReconnaissanceFire reconnaissanceFire;
    [SerializeField] private Ghost ghost;

    public override void Enter()
    {
        ghost.PassingThroughGhost(true);
        //ability.ActivateSkill(suppression);
        //reconnaissanceFire.ReconnaissanceFireAuraDarknesActive(true);
    }

    public override void Exit()
    {
        ghost.PassingThroughGhost(false);
        //ability.DeactivateSkill(suppression);
        //reconnaissanceFire.ReconnaissanceFireAuraDarknesActive(true);
    }
}
