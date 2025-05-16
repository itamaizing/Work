using Mirror;
using System.Collections;
using UnityEngine;

public class ScraderSpawn : Skill
{
    private Vector3 _spawnPoint = Vector3.positiveInfinity;

    [SerializeField] private SpawnComponent spawnComponent;
    [SerializeField] private MinionMove minionMove;
    [SerializeField] private MinionComponent minion;
    [SerializeField] private Tentacles tentacle;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => _spawnPoint != Vector3.positiveInfinity;

    public Tentacles Tentacle { get => tentacle; set => tentacle = value; }

    protected override void Awake()
    {
        base.Awake();
        minionMove.CanMove = false;
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
        if (targetInfo.Points.Count > 0)
            _spawnPoint = targetInfo.Points[0];
    }

    protected override IEnumerator CastJob()
    {
        if (minion.TryGetComponent<Character>(out var character))
        {
            if (character.SelectComponent != null)
                character.SelectComponent.Deselect();

            if (character.SelectedCircle != null)
                character.SelectedCircle.IsActive = false;

            if (character.TryGetComponent<MinimapMarker>(out var minimap) && minimap != null)
                minimap.IsActive = false;
        }

        Hero.Abilities.DeactivateSkill(this);

        if (tentacle.TryGetComponent<SpawnComponent>(out SpawnComponent spawnComponent))
        {
            spawnComponent.CmdSpawnUnitPoint(_spawnPoint, Quaternion.identity);
            spawnComponent.CmdRemoveUnit(minion);
        }

        Destroy(gameObject);
        yield return null;
    }

    protected override void ClearData() { }
}
