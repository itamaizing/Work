using UnityEngine;

public class LightingStrikeTalent : Talent
{
    [SerializeField] private LightningStrike _skill;
    [SerializeField] private SkillManager _skillManagerGangdollarff;
    public override void Enter()
    {
        _skillManagerGangdollarff.ActivateSkill(_skill);
    }

    public override void Exit()
    {
        _skillManagerGangdollarff.DeactivateSkill(_skill);
    }
}
