using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarknessTalent_3 : Talent
{
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private Suppression suppression;
    [SerializeField] private ReconnaissanceFire reconnaissanceFire;
    [SerializeField] private Ghost ghost;
    [SerializeField] private VisionComponent visionComponent;
    [SerializeField] private Skill skill;

    public override void Enter()
    {
        skillManager.ActivateSkill(suppression);
        reconnaissanceFire.ReconnaissanceFireAuraDarknesActive(true);
        ghost.MovingToGhostWithZeroMana(true);
        visionComponent.VisionRange += 3;
        skill.Radius += 1.5f;
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(suppression);
        reconnaissanceFire.ReconnaissanceFireAuraDarknesActive(true);
        ghost.MovingToGhostWithZeroMana(false);
        visionComponent.VisionRange -= 3;
        skill.Radius -= 1.5f;
    }
}
