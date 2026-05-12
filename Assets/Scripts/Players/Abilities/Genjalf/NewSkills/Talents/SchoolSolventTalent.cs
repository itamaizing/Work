using UnityEngine;

public class SchoolSolventTalent : Talent
{
    [SerializeField] private SchoolSolvent _solventSkill;

    public override void Enter()
    {
        if (_solventSkill != null)
            character.Abilities.ActivateSkill(_solventSkill);
    }

    public override void Exit()
    {
        if (_solventSkill != null)
            character.Abilities.DeactivateSkill(_solventSkill);
    }
}
