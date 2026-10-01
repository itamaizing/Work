using UnityEngine;

public class StingingHitsTalent : Talent
{
    [SerializeField] private StingingHitsPassive _stingingHits;

    public override void Enter()
    {
        character.Abilities.ActivateSkill(_stingingHits);
        _stingingHits?.EnableStingingHits(true);
    }

    public override void Exit()
    {
        _stingingHits?.EnableStingingHits(false);
        character.Abilities.DeactivateSkill(_stingingHits);
    }
}
