using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RisingOfShadows : Skill
{
    [SerializeField] private ShadowMinion _shadowPrefab;
    [SerializeField] private float _aoeRadius = 3f;
    [SerializeField] private float _shadowSpeedMultiplier = 0.5f;
    [SerializeField] private Vector3 _spawnOffset = new Vector3(0.5f, 0f, 0.5f);

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => CheckCanCast();

    private Vector3 _clickPoint;
    
    #region SpiritHealthOnShadow
    private bool _spiritHealthIsEnabled;
    public bool EnableSpiritHealth(bool val) => _spiritHealthIsEnabled = val;
    #endregion

    private bool CheckCanCast() =>
        Vector3.Distance(_clickPoint, transform.position) <= AreaInfo.Radius;

    private bool IsEnemyTarget(Character target) =>
        target.gameObject.layer == LayerMask.NameToLayer("Enemy");

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Points.Count > 0)
            _clickPoint = (Vector3)targetInfo.Points[0];
    }

    protected override void ClearData()
    {
        _clickPoint = Vector3.zero;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        while (!GetMouseButton)
            yield return null;

        _clickPoint = Targeting.GetMousePoint();
        targetInfo.Points.Add(_clickPoint);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        Collider[] hits = Physics.OverlapSphere(_clickPoint, _aoeRadius, Targeting.Layer);

        var targets = new List<GameObject>();
        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<Character>(out var target)) continue;
            if (!IsEnemyTarget(target)) continue;
            if (target.IsDead) continue;

            targets.Add(target.gameObject);
        }

        CmdSpawnShadows(_clickPoint, targets.ToArray());

        yield return null;
    }

    [Command]
    private void CmdSpawnShadows(Vector3 position, GameObject[] targets)
    {
        foreach (var targetGO in targets)
        {
            if (targetGO == null) continue;
            if (!targetGO.TryGetComponent<Character>(out var target)) continue;
            if (target.IsDead) continue;

            Vector3 spawnPos = target.transform.position +
                               new Vector3(
                                   UnityEngine.Random.Range(-_spawnOffset.x, _spawnOffset.x),
                                   0f,
                                   UnityEngine.Random.Range(-_spawnOffset.z, _spawnOffset.z)
                               );

            ShadowMinion shadow = Instantiate(_shadowPrefab, spawnPos, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(shadow.gameObject, _hero.NetworkSettings.MyRoom);
            NetworkServer.Spawn(shadow.gameObject, connectionToClient);

            TargetRpcInitShadow(connectionToClient, shadow.gameObject, targetGO, _shadowSpeedMultiplier);
        }
    }

    [TargetRpc]
    private void TargetRpcInitShadow(NetworkConnectionToClient conn, GameObject shadowGO, GameObject targetGO, float speedMultiplier)
    {
        if (shadowGO == null || targetGO == null) return;

        ShadowMinion shadow = shadowGO.GetComponent<ShadowMinion>();
        Character target    = targetGO.GetComponent<Character>();

        if (shadow == null || target == null) return;

        shadow.InitOnClient(target, this, speedMultiplier,false);
        shadow.IsApplySpiritHealth(_spiritHealthIsEnabled);
    }
}
