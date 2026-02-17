using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class protectiveCocoonSpawn : Skill
{
    [Header("Cocoon")]
    [SerializeField] private ProtectiveCocoon _cocoonPrefab;

    private Character _targetCharacter;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        FindTargetCharacter(false, false);

        var target = GetTempTargetCharacter();

        if (target == null)
            yield break;

        if (!target.TryGetComponent(out SwarmCapacity _))
            yield break;

        TargetInfo info = new TargetInfo();
        info.AddTarget(target);

        targetDataSavedCallback?.Invoke(info);

        yield break;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0)
        {
            _targetCharacter = targetInfo.GetTargets()[0] as Character;
        }
    }

    protected override IEnumerator CastJob()
    {
        if (_targetCharacter == null)
            yield break;

        if (!isServer)
        {
            CmdSpawnCocoon(_targetCharacter.netIdentity);
            yield break;
        }

        SpawnCocoon(_targetCharacter);

        yield break;
    }

    [Command]
    private void CmdSpawnCocoon(NetworkIdentity targetIdentity)
    {
        if (targetIdentity == null) return;

        var target = targetIdentity.GetComponent<Character>();
        if (target == null) return;

        SpawnCocoon(target);
    }

    private void SpawnCocoon(Character target)
    {
        if (target == null) return;

        if (!target.TryGetComponent(out SwarmCapacity _))
            return;

        Vector3 spawnPos = target.transform.position;

        var cocoon = Instantiate(_cocoonPrefab, spawnPos, Quaternion.identity);
        NetworkServer.Spawn(cocoon.gameObject);

        cocoon.Init(target);
    }

    protected override void ClearData()
    {
        _targetCharacter = null;
        ClearTempTarget();
    }
}
