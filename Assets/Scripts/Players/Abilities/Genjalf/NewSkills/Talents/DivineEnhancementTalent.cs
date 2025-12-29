using UnityEngine;

public class DivineEnhancementTalent : Talent
{
    [SerializeField] private DivineEnhancement _divineEnhancement;
    [SerializeField] private SkillManager _ability;
    
    public override void Enter()
    {
        _ability.ActivateSkill(_divineEnhancement);
    }

    public override void Exit()
    {
        _ability.DeactivateSkill(_divineEnhancement);
    }
}
