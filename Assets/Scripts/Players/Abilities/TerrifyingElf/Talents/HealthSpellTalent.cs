using UnityEngine;

public class HealthSpellTalent : Talent
{
    [SerializeField] private HealthSpell _healthSpell;
    [SerializeField] private SkillManager _ability;

    public override void Enter()
    {
        _ability.ActivateSkill(_healthSpell);
    }

    public override void Exit()
    {
        _ability.DeactivateSkill(_healthSpell);
    }
}

