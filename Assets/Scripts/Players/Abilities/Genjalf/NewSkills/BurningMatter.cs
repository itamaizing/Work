using System;
using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

public class BurningMatter : Skill
{
    [Header("Burning Matter Settings")]
    [SerializeField] private BurningMatterTile _burningMatterPrefab;
    [SerializeField] private float _duration = 6f;
    [SerializeField] private float _radius = 1.5f;

    public override string AdditionalDescription =>
        $"Радиус: {AbilityNameBox.ColorOpen}{_radius * 2f}м{AbilityNameBox.ColorEnd}\n" +
        $"Длительность: {AbilityNameBox.ColorOpen}{_duration} сек{AbilityNameBox.ColorEnd}";

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    protected override bool IsCanCast => CanCast();

    private bool CanCast()
    {
        return Vector3.Distance(Targeting.GetMousePoint(), transform.position) < AreaInfo.Radius;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo info = new TargetInfo();

        while (!Input.GetMouseButtonDown(0))
            yield return null;

        Vector3 targetPoint = Targeting.GetMousePoint();

        Vector3 direction = targetPoint - transform.position;
        if (direction.magnitude > AreaInfo.Radius)
            targetPoint = transform.position + direction.normalized * AreaInfo.Radius;

        info.Points.Add(targetPoint);
        callbackDataSaved(info);
    }

    protected override IEnumerator CastJob()
    {
        if (Targeting.GetMousePoint() == Vector3.zero)
            yield break;

        Vector3 spawnPoint = Targeting.GetMousePoint();

        CmdSpawnBurningMatter(spawnPoint);
        yield return null;
    }

    [Command]
    private void CmdSpawnBurningMatter(Vector3 position)
    {
        GameObject instance = Instantiate(_burningMatterPrefab.gameObject, position, Quaternion.identity);
        NetworkServer.Spawn(instance, connectionToClient);

        var area = instance.GetComponent<BurningMatterTile>();
        area.Init(_duration, _radius, _hero);
        RcInitSpawnBurningMatter(area.gameObject);
    }

    private void RcInitSpawnBurningMatter(GameObject instance)
    {
        if(instance == null) return;
        BurningMatterTile area = instance.GetComponent<BurningMatterTile>();
        area.Init(_duration,_radius,_hero);
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
    }
}
