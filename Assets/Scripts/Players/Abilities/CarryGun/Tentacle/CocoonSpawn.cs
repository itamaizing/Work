using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CocoonSpawn : Skill
{
    private Vector3 _spawnPoint = Vector3.positiveInfinity;

    [SerializeField] private SpawnComponent spawnComponent;
    [SerializeField] private MinionMove minionMove;
    [SerializeField] private MinionComponent minion;
    [SerializeField] private Tentacles tentacle;
    [SerializeField] private int maxSpawn = 5;

    private int _currentSpawnCount;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => true;

    public Tentacles Tentacle { get => tentacle; set => tentacle = value; }
    public int CurrentSpawnCount
    {
        get => _currentSpawnCount;
        set => _currentSpawnCount = Mathf.Max(0, value);
    }

    private void OnEnable()
    {
        Hero.Move.CanMove = false;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Targets.Add(Hero);
        callbackDataSaved(targetInfo);
        yield return null;
    }
    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo == null) return;
        if (targetInfo.Targets.Contains(Hero)) return;
        targetInfo.Targets.Add(Hero);
    }

    protected override IEnumerator CastJob()
    {
        if (tentacle.TryGetComponent<SpawnComponent>(out var spawnComponent))
        {
            if (_currentSpawnCount >= maxSpawn) yield break;

            if (TryGetValidSpawnPoint(out Vector3 spawnPos))
            {
                Debug.Log($" _currentSpawnCount {_currentSpawnCount}");
                spawnComponent.CmdSpawnEnemyPoint(spawnPos, Quaternion.identity, minion, 1, false, Hero);
            }

            CmdTentacleCocoon(spawnComponent);
        }

         yield return null;
    }
    private Vector3 GetRandomOffsetPosition(Vector3 center, float radius)
    {
        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2);
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
        return center + offset;
    }

    private bool TryGetValidSpawnPoint(out Vector3 result)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 testPos = GetRandomOffsetPosition(transform.position, 1.6f);
            if (!IsOccupied(testPos))
            {
                result = testPos;
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }

    private bool IsOccupied(Vector3 point)
    {
        Collider[] colliders = Physics.OverlapSphere(point, 1);
        foreach (var collider in colliders)
        {
            if (collider.TryGetComponent<MinionComponent>(out var minion))
            {
                if (minion.GetComponent<ScraderSpawn>() != null) return true;
            }
        }

        return false;
    }

    [Command]
    private void CmdTentacleCocoon(SpawnComponent spawnComponent)
    {
        RpcTentacleCocoon(spawnComponent);
    }


    [ClientRpc]
    private void RpcTentacleCocoon(SpawnComponent spawnComponent)
    {
        foreach (var cocoon in spawnComponent.Units) if (cocoon.TryGetComponent<ScraderSpawn>(out ScraderSpawn scraderSpawn)) scraderSpawn.Tentacle = tentacle;
    }

    protected override void ClearData() { }
}
