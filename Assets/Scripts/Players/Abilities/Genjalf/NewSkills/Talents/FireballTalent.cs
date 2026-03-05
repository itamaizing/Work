using UnityEngine;

public class FireballTalent : Talent
{
    [SerializeField] private SkillManager _skillManager;
    [SerializeField] private FireBallSkill _fireBallSkill;
    
    public override void Enter()
    {
        _skillManager.ActivateSkill(_fireBallSkill);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_fireBallSkill);
    }
}
