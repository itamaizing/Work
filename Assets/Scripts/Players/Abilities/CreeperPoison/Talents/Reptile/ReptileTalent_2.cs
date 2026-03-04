using UnityEngine;

public class ReptileTalent_2 : Talent
{
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private LightningMovement lightningMovement;

    public override void Enter()
    {
        skillManager.ActivateSkill(lightningMovement);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(lightningMovement);
    }
}
