using UnityEngine;

public class DarkFormTalent : Talent
{
    [SerializeField] private SkillManager _skillManager;
    [SerializeField] private DarkForm _darkForm;

    public override void Enter()
    {
        _skillManager.ActivateSkill(_darkForm);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_darkForm);
    }
}
