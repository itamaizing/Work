using UnityEngine;

public class RetributionLightTalent : Talent
{
    [SerializeField] private SkillManager _skillManager;
    [SerializeField] private RetributionLight _retributionLight;
    
    public override void Enter()
    {
        _skillManager.ActivateSkill(_retributionLight);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_retributionLight);
    }
}
