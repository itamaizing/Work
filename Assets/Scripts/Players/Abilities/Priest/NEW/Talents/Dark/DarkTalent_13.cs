using UnityEngine;

public class DarkTalent_13 : Talent
{
    [SerializeField] private SkillManager _skillManager;
    [SerializeField] private MergeWithDarknessSkill _mergeWithDarkness;


    public override void Enter()
    {
        _skillManager.ActivateSkill(_mergeWithDarkness);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_mergeWithDarkness);
    }
}
