using UnityEngine;
using UnityEngine.Serialization;

public class PillarOfLightTalent : Talent
{
    [SerializeField] private SkillManager _skillManager;
    [SerializeField] private PillarOfLight _pillarOfLight;
    
    public override void Enter()
    {
        _skillManager.ActivateSkill(_pillarOfLight);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_pillarOfLight);
    }
}
