using UnityEngine;

public class TransformationTalent : Talent
{
    [SerializeField] private TransformationSkill _skillTransformation;
    [SerializeField] private SkillManager _ability;

    public override void Enter()
    {
        _ability.ActivateSkill(_skillTransformation);
    }

    public override void Exit()
    {
        _ability.DeactivateSkill(_skillTransformation);
    }
}
