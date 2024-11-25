using UnityEngine;

public class MetabolismReptileTalent : Talent
{
    [SerializeField] private MetabolismReptile _metabolismReptile;
    [SerializeField] private SkillManager _ability;

    public override void Enter()
    {
        if (_ability.Abilities.Contains(_metabolismReptile))
        {
            _metabolismReptile.enabled = true;
        }
        else
        {
            //_ability.AddAbility(_metabolismReptile);
        }
    }

    public override void Exit()
    {
        if (_ability.Abilities.Contains(_metabolismReptile))
        {
            //_ability.RemoveAbility(_metabolismReptile);
            _metabolismReptile.enabled = false;
        }
        else
        {
            _metabolismReptile.enabled = false;
        }
    }
}
