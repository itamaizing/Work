using Mirror;
using System.Collections;
using UnityEngine;

public class ScraderSpawn : Skill
{
    private Vector3 _spawnPoint = Vector3.positiveInfinity;
    private Character _target = null;

    private SpawnComponent _carryGunSpawnComponent;
    private CarryGunAura _carryGunAura;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => _spawnPoint != Vector3.positiveInfinity;

    public void Setup(SpawnComponent carryGunSpawnComponent, CarryGunAura carryGunAura)
    {
        _carryGunSpawnComponent = carryGunSpawnComponent;
        _carryGunAura = carryGunAura;
    }

    protected override IEnumerator PrepareJob(System.Action<TargetInfo> callback)
    {
        _skillRender.DrawRadius(_radius);
        while (!GetMouseButton) yield return null;

        TargetInfo info = new TargetInfo();
        info.Points.Add(transform.position);
        callback?.Invoke(info);
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Points.Count > 0) _spawnPoint = targetInfo.Points[0];
        if (targetInfo.Targets.Count > 0 && targetInfo.Targets[0] is Character character) _target = character;
    }

    protected override IEnumerator CastJob()
    {
        if (_carryGunSpawnComponent != null && isServer)
        {
            _carryGunSpawnComponent.CmdSpawnUnitPoint(_spawnPoint, Quaternion.identity);

            yield return new WaitForSeconds(0.1f);

            if (_carryGunSpawnComponent.Units.Count > 0)
            {
                var spawnedCharacter = _carryGunSpawnComponent.Units[^1];

                if (spawnedCharacter is MinionComponent spawnedMinion)
                {
                    _carryGunAura.AddToSwarm(spawnedMinion);
                }
            }

            _carryGunAura.UnsubscribeScraderSpawn(this);
            NetworkServer.Destroy(gameObject);
        }
        yield return null;
    }

    protected override void ClearData() { }
}
