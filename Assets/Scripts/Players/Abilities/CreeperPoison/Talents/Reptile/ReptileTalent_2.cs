using UnityEngine;

public class ReptileTalent_2 : Talent
{
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private LightningMovement lightningMovement;
    [SerializeField] private ColdBlood coldBlood;

    public override void Enter()
    {
        skillManager.ActivateSkill(coldBlood);
        skillManager.ActivateSkill(lightningMovement);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(coldBlood);
        skillManager.DeactivateSkill(lightningMovement);
    }
}
