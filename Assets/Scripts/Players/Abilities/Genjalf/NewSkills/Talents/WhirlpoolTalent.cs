using UnityEngine;

public class WhirlpoolTalent : Talent
{
    [SerializeField] private WhirlpoolSkill _whirlpool;
    [SerializeField] private SkillManager _skillManager;


    public override void Enter()
    {
        _skillManager.ActivateSkill(_whirlpool);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_whirlpool);
    }
}
