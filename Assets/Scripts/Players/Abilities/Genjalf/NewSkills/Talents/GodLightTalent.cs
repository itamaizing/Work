using UnityEngine;

public class GodLightTalent : Talent
{
    [SerializeField] private GodLight _skillGodLight;
    [SerializeField] private SkillManager _ability;

    public override void Enter()
    {
        _ability.ActivateSkill(_skillGodLight);
    }

    public override void Exit()
    {
        _ability.DeactivateSkill(_skillGodLight);
    }
}
