using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SpawnType
{
    None = -1,
    Scrader = 0,
    Spisnacider = 1,
    Getomir = 2,
}

public class CreatureSpawn : Skill
{
    private Vector3 _spawnPoint = Vector3.positiveInfinity;

    [SerializeField] private SpawnComponent spawnComponent;
    [SerializeField] private MinionMove minionMove;
    [SerializeField] private MinionComponent minion;
    [SerializeField] private Tentacles tentacle;

    private SpawnType _spawnType = SpawnType.None;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => _spawnPoint != Vector3.positiveInfinity;

    public SpawnType SpawnType { get => _spawnType; set => _spawnType = value; }
    public Tentacles Tentacle { get => tentacle; set => tentacle = value; }

    private void OnEnable()
    {
        minionMove.SetCanMove(false);
    }

    private void OnDisable()
    {
        if (_spawnType == SpawnType.Getomir && tentacle != null)
        {
            tentacle.OnSpawnGetomirChanged -= HandleSpawnGetomirChanged;
        }
    }

    private void Start()
    {
        if (_spawnType == SpawnType.Getomir && tentacle != null)
        {
            tentacle.OnSpawnGetomirChanged += HandleSpawnGetomirChanged;
        }
    }

    private void HandleSpawnGetomirChanged(bool isActive)
    {
        if (_spawnType != SpawnType.Getomir) return;
        if (Hero == null) return;

        var skillManager = Hero.Abilities;
        if (skillManager == null) return;

        if (isActive) skillManager.ActivateSkill(this);
        else skillManager.DeactivateSkill(this);
    }

    private Vector3 GetRandomOffsetPosition(Vector3 center, float radius)
    {
        float angle = Random.Range(0f, Mathf.PI * 2);
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
        return center + offset;
    }

    protected override IEnumerator PrepareJob(System.Action<TargetInfo> callback)
    {
        TargetInfo info = new TargetInfo();
        info.Points.Add(transform.position);
        callback?.Invoke(info);

        yield break;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Points.Count > 0)
            _spawnPoint = targetInfo.Points[0];
    }

    protected override IEnumerator CastJob()
    {
        while (_spawnType == SpawnType.None) yield return null;

        int index = (int)_spawnType;

        if (minion.TryGetComponent<Character>(out var character))
        {
            character.SelectComponent?.Deselect();
            character.SelectedCircle?.SwitchClostestTarget(false);
            character.SelectedCircle.gameObject.SetActive(false);

            if (character.TryGetComponent<MinimapMarker>(out var minimap)) minimap.IsActive = false;

            var states = new List<AbstractCharacterState>(character.CharacterState.CurrentStates);
            foreach (var state in states) character.CharacterState.RemoveState(state.State);
        }

        if (tentacle.TryGetComponent<SpawnComponent>(out var spawnComponent))
        {
            Vector3 spawnPos = GetRandomOffsetPosition(_spawnPoint, 1.6f);

            spawnComponent.CmdSpawnAliesPoint(spawnPos, Quaternion.identity, minion, index, false, tentacle.Hero);

            CmdTentacleCocoon(spawnComponent.netIdentity);
        }

        yield return null;
    }

    [Command]
    private void CmdTentacleCocoon(NetworkIdentity spawnIdentity)
    {
        RpcTentacleCocoon(spawnIdentity);
    }

    [ClientRpc]
    private void RpcTentacleCocoon(NetworkIdentity spawnIdentity)
    {
        if (spawnIdentity == null) return;

        var spawnComponent = spawnIdentity.GetComponent<SpawnComponent>();
        if (spawnComponent == null) return;

        foreach (var unit in spawnComponent.Units)
        {
            if (unit == null) continue;

            foreach (var spawn in unit.GetComponents<CreatureCarryGun>())
            {
                spawn.DadSkill = tentacle;
            }
        }
    }

    protected override void ClearData()
    {
        _spawnType = SpawnType.None;
    }
}
