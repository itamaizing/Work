using UnityEngine;

public class RadianceOfLightTalent : Talent
{
    [SerializeField] private SkillManager _skillManager;
    [SerializeField] private RadianceOfLight _radianceOfLight;

    public override void Enter()
    {
        _skillManager.ActivateSkill(_radianceOfLight);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_radianceOfLight);
    }
}
