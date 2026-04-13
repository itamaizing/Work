using UnityEngine;

public class SwarmTalent_13 : Talent
{
    [SerializeField] private SummoningSwarm _summoningSwarm;
    [SerializeField] private SkillManager _skillManager;

    public override void Enter()
    {
        _skillManager.ActivateSkill(_summoningSwarm);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_summoningSwarm);
    }
}