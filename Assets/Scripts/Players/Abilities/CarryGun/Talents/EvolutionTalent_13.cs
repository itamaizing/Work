using UnityEngine;

public class EvolutionTalent_13 : Talent
{
    [SerializeField] private RechargeGlands _rechargeGlands;
    [SerializeField] private SkillManager _skillManager;

    public override void Enter()
    {
        _skillManager.ActivateSkill(_rechargeGlands);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_rechargeGlands);
    }
}
