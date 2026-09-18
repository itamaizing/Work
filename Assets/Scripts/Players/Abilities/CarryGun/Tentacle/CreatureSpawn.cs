using System;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

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
    [SerializeField] private WombSpawn wombSpawn;

    private SpawnType _spawnType = SpawnType.None;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => _spawnPoint != Vector3.positiveInfinity;
    
    public override object GroupKey => (GetType(), SpawnType);

    public SpawnType SpawnType
    {
        get => _spawnType;
        set
        {
            Debug.Log($"[CreatureSpawn:{GetInstanceID()}] SpawnType set attempt: {_spawnType} -> {value}");
            if (_spawnType == value) return;
            _spawnType = value;
            Debug.Log($"[CreatureSpawn:{GetInstanceID()}] SpawnType CHANGED, firing OnSpawnTypeChanged({value})");
            OnSpawnTypeChanged?.Invoke(value);
        }
    }
    public WombSpawn WombSpawn { get => wombSpawn; set => wombSpawn = value; }
    
    public event Action<SpawnType> OnSpawnTypeChanged;

    private void OnEnable()
    {
        minionMove.SetCanMove(false);
    }

    private void OnDisable()
    {
        if (_spawnType == SpawnType.Getomir && wombSpawn != null)
        {
            wombSpawn.OnSpawnGetomirChanged -= HandleSpawnGetomirChanged;
        }
    }

    private void Start()
    {
        if (_spawnType == SpawnType.Getomir && wombSpawn != null)
        {
            wombSpawn.OnSpawnGetomirChanged += HandleSpawnGetomirChanged;
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

            var states = new List<StateBasic>(character.CharacterState.CurrentStates);
            foreach (var state in states) character.CharacterState.RemoveState(state.State);
        }

        if (wombSpawn.TryGetComponent<SpawnComponent>(out var spawnComponent))
        {
            Vector3 spawnPos = GetRandomOffsetPosition(_spawnPoint, 1.6f);

            CmdSpawnAndTentacleCocoon(spawnComponent.netIdentity, spawnPos, index, wombSpawn.Hero);
        }

        yield return null;
    }

    [Command]
    private void CmdSpawnAndTentacleCocoon(NetworkIdentity spawnIdentity, Vector3 position, int index, Character parentCharacter)
    {
        if (spawnIdentity == null) return;

        var spawnComponentServer = spawnIdentity.GetComponent<SpawnComponent>();
        if (spawnComponentServer == null) return;

        var spawned = spawnComponentServer.SpawnAliesPointServer(position, Quaternion.identity, minion, index, false, parentCharacter);
        if (spawned == null) return;

        RpcTentacleCocoon(spawned.netIdentity);
    }

    [ClientRpc]
    private void RpcTentacleCocoon(NetworkIdentity spawnedUnitIdentity)
    {
        if (spawnedUnitIdentity == null) return;

        foreach (var spawn in spawnedUnitIdentity.GetComponents<CreatureCarryGun>())
        {
            spawn.DadSkill = wombSpawn;
        }
    }

    protected override void ClearData()
    {
        Debug.Log($"[CreatureSpawn:{GetInstanceID()}] ClearData called, current _spawnType={_spawnType}");
        SpawnType = SpawnType.None;
    }
}
