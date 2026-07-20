using UnityEngine;

public class MetabolismReptileTalent : Talent
{
    [SerializeField] MetabolismReptile _metabolismReptile;
    [SerializeField] private SkillManager _skillManager;

    public override void Enter()
    {
        _skillManager.ActivateSkill(_metabolismReptile);

    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_metabolismReptile);

    }
}
