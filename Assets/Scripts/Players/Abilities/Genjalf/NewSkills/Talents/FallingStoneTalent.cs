using UnityEngine;

public class FallingStoneTalent : Talent
{
    [SerializeField] private StoneFromSky _stoneFromSky;
    [SerializeField] private SkillManager _skillManager;
    public override void Enter()
    {
        _skillManager.ActivateSkill(_stoneFromSky);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_stoneFromSky);
    }
}
