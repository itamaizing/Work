using UnityEngine;

public class NinjaPoisonTalent_3 : Talent
{
    [SerializeField] private CreeperInvisible creeperInvisible;
    [SerializeField] private SkillManager manager;

    public override void Enter()
    {
        manager.ActivateSkill(creeperInvisible);
        creeperInvisible.EnableEnemyCheck();
    }

    public override void Exit()
    {
        creeperInvisible.DisableEnemyCheck();
        manager.DeactivateSkill(creeperInvisible);
    }
}