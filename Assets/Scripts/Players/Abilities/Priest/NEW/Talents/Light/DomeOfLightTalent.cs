using UnityEngine;
using UnityEngine.Serialization;

public class DomeOfLightTalent : Talent
{
    [SerializeField] private SkillManager _skillManager;
    [SerializeField] private DomeOfLight _domeOfLight;
    
    public override void Enter()
    {
        _skillManager.ActivateSkill(_domeOfLight);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_domeOfLight);
    }
}
