using UnityEngine;

public class RetributionTalent : Talent
{
    [SerializeField] private SkillManager _skillManager;
    [SerializeField] private RetributionSkill _retribution;
    
    public override void Enter()
    {
        _skillManager.ActivateSkill(_retribution);
        
        _retribution.OnActive(true);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_retribution);
        
        _retribution.OnActive(false);
    }
}
