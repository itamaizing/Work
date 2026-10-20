using UnityEngine;

public class LightTalent_10 : Talent
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
