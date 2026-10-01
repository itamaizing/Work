using Mirror;
using UnityEngine;

public class HellZone : NetworkBehaviour
{
    [SerializeField] private Transform _heroSpawnPoint;
    [SerializeField] private Transform _targetSpawnPoint;
    [SerializeField] public BoxCollider CameraBounds;

    public Vector3 HeroSpawn   => _heroSpawnPoint   ? _heroSpawnPoint.position   : transform.position + Vector3.left  * 2f;
    public Vector3 TargetSpawn => _targetSpawnPoint ? _targetSpawnPoint.position : transform.position + Vector3.right * 2f;
}