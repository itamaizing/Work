using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class ProtectiveCocoonSpawn : Skill
{
    [Header("Cocoon")]
    [SerializeField] private ProtectiveCocoon _cocoonPrefab;

    private Character _targetCharacter;

    private const float TargetSearchRadius = 0.5f;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    protected override bool IsCanCast => CheckIsCanCast();

    private bool CheckIsCanCast()
    {
        return GetTarget() != null &&
               Vector3.Distance(GetTarget().Transform.position, transform.position) <= Radius &&
               NoObstacles(GetTarget().Transform.position, transform.position, _obstacle);
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        while (GetTempTarget() == null)
        {
            if (GetMouseButton)
            {
                FindTarget(TargetSearchRadius, GetMousePoint());

                if (GetTempTarget() != null)
                {
                    if (!(GetTempTarget() is Character character))
                    {
                        ClearTempTarget();
                    }
                    else
                    {
                        if (!character.TryGetComponent(out SwarmCapacity _))
                        {
                            ClearTempTarget();
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            yield return null;
        }

        SetTarget(GetTempTarget());
        _targetCharacter = GetTarget() as Character;

        if (_targetCharacter == null)
            yield break;

        targetInfo.Points.Add(_targetCharacter.transform.position);
        targetInfo.AddTarget(_targetCharacter);

        callbackDataSaved.Invoke(targetInfo);
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0)
            SetTarget(targetInfo.GetTargets()[0]);

        _targetCharacter = GetTarget() as Character;
    }

    protected override IEnumerator CastJob()
    {
        if (_targetCharacter == null)
            yield break;

        if (!IsCanCast)
            yield break;

        if (!isServer)
        {
            CmdSpawnCocoon(_targetCharacter.netIdentity);
            yield break;
        }

        SpawnCocoon(_targetCharacter);
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
        ClearTarget();
        ClearTempTarget();
        _targetCharacter = null;
    }
}
