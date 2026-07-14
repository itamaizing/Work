using UnityEngine;

public class ExplosionCloudEnabled : Talent
{
    [SerializeField] ExplosionPoisonCloud _explosionCloud;
    [SerializeField] private SkillManager _skillManager;

    public override void Enter()
    {
        _skillManager.ActivateSkill(_explosionCloud);
        
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_explosionCloud);
        
    }
}
