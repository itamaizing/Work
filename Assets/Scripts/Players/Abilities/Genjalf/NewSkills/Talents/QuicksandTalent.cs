using UnityEngine;

public class QuicksandTalent : Talent
{
    [SerializeField] private Quicksand _quicksand;
    [SerializeField] private SkillManager _ability;

    public override void Enter()
    {
        _ability.ActivateSkill(_quicksand);
    }

    public override void Exit()
    {
        _ability.DeactivateSkill(_quicksand);
    }
}
