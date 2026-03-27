using UnityEngine;

public class ReptileTalent_3 : Talent
{
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private PoisonSlap _poisonSlap;

    public override void Enter()
    {
        skillManager.ActivateSkill(_poisonSlap);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(_poisonSlap);
    }
}
