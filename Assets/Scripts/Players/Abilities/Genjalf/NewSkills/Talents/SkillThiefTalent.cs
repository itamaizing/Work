using UnityEngine;

public class SkillThiefTalent : Talent
{
    [SerializeField] private SpellThiefSkill _skillThief;
    [SerializeField] private SkillManager _ability;

    public override void Enter()
    {
        _ability.ActivateSkill(_skillThief);
    }

    public override void Exit()
    {
        _ability.DeactivateSkill(_skillThief);
    }
}
