using UnityEngine;

public class SummonTalent : Talent
{
    [SerializeField] private SkillManager _skillManager;
    [SerializeField] private ElementalSpawn _elementalSpawn;

    public override void Enter()
    {
        _skillManager.ActivateSkill(_elementalSpawn);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_elementalSpawn);
    }
}
