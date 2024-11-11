using UnityEngine;

public class LightShieldSkillTalent : Talent
{
    [SerializeField] private LightShieldSkill _lightShieldSkill;
    [SerializeField] private SkillManager _ability;

    public override void Enter()
    {
        _ability.ActivateSkill(_lightShieldSkill);
    }

    public override void Exit()
    {
        _ability.DeactivateSkill(_lightShieldSkill);
    }
}

