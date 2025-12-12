using UnityEngine;

public class DarkTalent_11 : Talent
{
    [SerializeField] Dark1PassiveSkill _darkPassiveSkill;
    [SerializeField] SkillManager _manager;

    public override void Enter()
    {
        _manager.ActivateSkill(_darkPassiveSkill);
    }

    public override void Exit()
    {
        _manager.DeactivateSkill(_darkPassiveSkill);
    }
}
