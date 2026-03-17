using UnityEngine;

public class SwarmTalent_11 : Talent
{
    [SerializeField] private Tentacles _tentacles;

    public override void Enter()
    {
        _tentacles.SpawnSpikeMucus(true);
    }

    public override void Exit()
    {
        _tentacles.SpawnSpikeMucus(false);
    }
}