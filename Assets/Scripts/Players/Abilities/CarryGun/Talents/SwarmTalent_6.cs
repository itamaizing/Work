using UnityEngine;

public class SwarmTalent_6 : Talent
{
    [SerializeField] private SwarmSpeedAura _swarmSpeedAura;
    [SerializeField] private SkillManager _skillManager;

    public override void Enter()
    {
        _skillManager.ActivateSkill(_swarmSpeedAura);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_swarmSpeedAura);
    }
}