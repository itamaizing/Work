using UnityEngine;

public class GodAuraTalent : Talent
{
    [SerializeField] private GodAuraSkill _godAuraSkill;

    public override void Enter()
    {
        _godAuraSkill.OnAuraEnabled(character.gameObject);
    }

    public override void Exit()
    {
        _godAuraSkill.OnAuraDisabled(character.gameObject);
    }
}
