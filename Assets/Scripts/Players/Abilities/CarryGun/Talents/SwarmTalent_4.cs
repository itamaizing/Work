using UnityEngine;

public class SwarmTalent_4 : Talent
{
    [SerializeField] private Tentacles _tentacles;

    public override void Enter()
    {
        _tentacles.WombSpreadsMucus(true);
        _tentacles.EffectTentaclesCreatures(true);
    }

    public override void Exit()
    {
        _tentacles.WombSpreadsMucus(false);
        _tentacles.EffectTentaclesCreatures(false);
    }
}
