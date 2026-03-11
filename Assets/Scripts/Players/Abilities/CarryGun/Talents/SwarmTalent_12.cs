using UnityEngine;

public class SwarmTalent_12 : Talent
{
    [SerializeField] private Tentacles _tentacles;

    public override void Enter()
    {
        _tentacles.SpawnGetomir(true);
    }

    public override void Exit()
    {
        _tentacles.SpawnGetomir(false);
    }
}
