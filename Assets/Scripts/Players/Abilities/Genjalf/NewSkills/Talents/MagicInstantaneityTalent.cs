using UnityEngine;

public class MagicInstantaneityTalent : Talent
{
    [SerializeField] private SkillManager _skillManager;
    [SerializeField] private MagicInstantaneity _magicInstantaneity;
    
    public override void Enter()
    {
        _skillManager.ActivateSkill(_magicInstantaneity);
        _magicInstantaneity.OnActive();
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_magicInstantaneity);
        _magicInstantaneity.OnDiactive();
    }
}
