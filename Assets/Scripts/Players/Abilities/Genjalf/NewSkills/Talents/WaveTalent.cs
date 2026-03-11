using UnityEngine;

public class WaveTalent : Talent
{
    [SerializeField] private WaveSkill _skillWave;
    [SerializeField] private SkillManager _ability;

    public override void Enter()
    {
        _ability.ActivateSkill(_skillWave);
    }

    public override void Exit()
    {
        _ability.DeactivateSkill(_skillWave);
    }
}
