using UnityEngine;

public class RisingShadowTalent : Talent
{
    [SerializeField] private SkillManager _skillManager;
    [SerializeField] private RisingOfShadows _risingOfShadows;


    public override void Enter()
    {
        _skillManager.ActivateSkill(_risingOfShadows);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_risingOfShadows);
    }
}
