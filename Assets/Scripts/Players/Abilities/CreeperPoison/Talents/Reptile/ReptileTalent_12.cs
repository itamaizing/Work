using UnityEngine;

public class ReptileTalent_12 : Talent
{
    [SerializeField] private ReptilianStasis _reptilianStasis;
    [SerializeField] private SkillManager _skillManager;

    public override void Enter()
    {
        _skillManager.ActivateSkill(_reptilianStasis);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_reptilianStasis);
    }
}

