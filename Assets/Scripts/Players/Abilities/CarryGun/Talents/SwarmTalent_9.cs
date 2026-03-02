using UnityEngine;

public class SwarmTalent_9 : Talent
{
    [SerializeField] private Tentacles _tentacles;

    public override void Enter()
    {
        _tentacles.SpawnSpike(true);
    }

    public override void Exit()
    {
        _tentacles.SpawnSpike(false);
    }
}
