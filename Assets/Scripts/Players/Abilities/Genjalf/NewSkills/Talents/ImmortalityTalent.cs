using UnityEngine;

public class ImmortalityTalent : Talent
{
    [SerializeField] private SkillManager _skillManager;
    [SerializeField] private ImmortalitySkill _immortalitySkill;
    public override void Enter()
    {
        _skillManager.ActivateSkill(_immortalitySkill);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_immortalitySkill);
    }
}
