using UnityEngine;

public class SwarmTalent_12 : Talent
{
    [SerializeField] private WombSpawn _wombSpawn;

    public override void Enter()
    {
        _wombSpawn.SpawnGetomir(true);
    }

    public override void Exit()
    {
        _wombSpawn.SpawnGetomir(false);
    }
}
