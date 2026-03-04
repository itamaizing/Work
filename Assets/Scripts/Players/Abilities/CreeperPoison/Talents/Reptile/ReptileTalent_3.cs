using UnityEngine;

public class ReptileTalent_3 : Talent
{
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private CreeperInvisible creeperInvisible;

    public override void Enter()
    {
        skillManager.ActivateSkill(creeperInvisible);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(creeperInvisible);
    }
}
