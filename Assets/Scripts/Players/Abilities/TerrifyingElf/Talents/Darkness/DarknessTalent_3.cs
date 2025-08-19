using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarknessTalent_3 : Talent
{
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private Suppression suppression;
    [SerializeField] private ReconnaissanceFire reconnaissanceFire;

    public override void Enter()
    {
        skillManager.ActivateSkill(suppression);
        reconnaissanceFire.ReconnaissanceFireAuraDarknesActive(true);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(suppression);
        reconnaissanceFire.ReconnaissanceFireAuraDarknesActive(true);
    }
}
