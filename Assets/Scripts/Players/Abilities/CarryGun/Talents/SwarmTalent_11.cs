using UnityEngine;

public class SwarmTalent_11 : Talent
{
    [SerializeField] private WombSpawn _wombSpawn;

    public override void Enter()
    {
        _wombSpawn.SpawnSpikeMucus(true);
    }

    public override void Exit()
    {
        _wombSpawn.SpawnSpikeMucus(false);
    }
}