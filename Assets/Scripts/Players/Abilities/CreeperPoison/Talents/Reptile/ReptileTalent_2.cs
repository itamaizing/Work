using UnityEngine;

public class ReptileTalent_2 : Talent
{
    [SerializeField] private GrabTongue _grabTongue;
    [SerializeField] private SkillManager _skillManager;

    public override void Enter()
    {
        _skillManager.ActivateSkill(_grabTongue);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_grabTongue);
    }
}
