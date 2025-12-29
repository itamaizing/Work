using UnityEngine;

public class CounterSpellTalent : Talent
{
    [SerializeField] private CounterSpell _counterSpell;
    [SerializeField] private SkillManager _ability;
    public override void Enter()
    {
        _ability.ActivateSkill(_counterSpell);
    }

    public override void Exit()
    {
        _ability.DeactivateSkill(_counterSpell);
    }
}
