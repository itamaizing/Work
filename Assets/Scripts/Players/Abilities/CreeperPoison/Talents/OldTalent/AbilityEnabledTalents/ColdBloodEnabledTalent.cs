using UnityEngine;

public class ColdBloodEnabledTalent : Talent
{
    [SerializeField] private ColdBlood _coldBlood;
    [SerializeField] private SkillManager _skillManager;

    public override void Enter()
    {
        _skillManager.ActivateSkill(_coldBlood);

    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_coldBlood);
        
    }
}
