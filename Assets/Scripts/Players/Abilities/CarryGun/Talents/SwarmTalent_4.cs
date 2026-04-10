using UnityEngine;

public class SwarmTalent_4 : Talent
{
    [SerializeField] private WombSpawn _wombSpawn;

    public override void Enter()
    {
        _wombSpawn.WombSpreadsMucus(true);
        _wombSpawn.EffectTentaclesCreatures(true);
    }

    public override void Exit()
    {
        _wombSpawn.WombSpreadsMucus(false);
        _wombSpawn.EffectTentaclesCreatures(false);
    }
}
