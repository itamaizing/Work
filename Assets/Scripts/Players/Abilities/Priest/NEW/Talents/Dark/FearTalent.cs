using UnityEngine;

public class FearTalent : Talent
{
    [SerializeField] private SkillManager _skillManager;
    [SerializeField] private FearSkill _fear;


    public override void Enter()
    {
        _skillManager.ActivateSkill(_fear);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_fear);
    }
}
