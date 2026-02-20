using UnityEngine;

public class GangInvisibleTalent : Talent
{
    [SerializeField] private GangInvisibleSkill _skillInvisible;
    [SerializeField] private SkillManager _ability;

    public override void Enter()
    {
        _ability.ActivateSkill(_skillInvisible);
    }

    public override void Exit()
    {
        _ability.DeactivateSkill(_skillInvisible);
    }
}
