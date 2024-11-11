using UnityEngine;

public class TestDispelTalent : Talent
{
    [SerializeField] private TestDispel _testDispel;
    [SerializeField] private SkillManager _ability;

    public override void Enter()
    {
        _ability.ActivateSkill(_testDispel);
    }

    public override void Exit()
    {
        _ability.DeactivateSkill(_testDispel);
    }
}
