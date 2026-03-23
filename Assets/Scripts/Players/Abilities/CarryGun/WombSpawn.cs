using Mirror;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class WombSpawn : Skill
{
    [SerializeField] private SpawnComponent _spawnComponent;
    [SerializeField] private Character _player;
    [SerializeField] private Tentacles _tentacle;

    private readonly List<GameObject> _spawnedWombs = new();

    private Vector3 _spawnPoint = Vector3.positiveInfinity;

    protected override int AnimTriggerCast => Animator.StringToHash("Spell");
    protected override int AnimTriggerCastDelay => 0;

    protected override bool IsCanCast =>
        IsValid(_spawnPoint);


    #region Talent
    private bool _isCocoonSpawnTalent = false;
    private bool _isProtectiveCooconSpawn = false;
    private bool _isProtectiveCooconSpawnAttack = false;
    private bool _isWombSpreadsMucus = false;
    private bool _isWombSpreadParasites = false;
    private bool _isSpawnGetomir;
    private bool _isSpawnSpikeMucus = false;

    public event Action<bool> OnSpawnGetomirChanged;
    public event Action<bool> OnWombSpreadsMucusChanged;
    public event Action<bool> OnWombSpreadsParasitesChanged;
    //public event Action<bool> OnSpawnSpikeMucus;

    public bool IsSpawnGetomir
    {
        get => _isSpawnGetomir;
        set
        {
            if (_isSpawnGetomir == value) return;

            _isSpawnGetomir = value;
            OnSpawnGetomirChanged?.Invoke(_isSpawnGetomir);
        }
    }

    public bool IsWombSpreadsMucus
    {
        get => _isWombSpreadsMucus;
        set
        {
            if (_isWombSpreadsMucus == value) return;

            _isWombSpreadsMucus = value;
            OnWombSpreadsMucusChanged?.Invoke(_isWombSpreadsMucus);
        }
    }

    public bool IsWombSpreadsParasites
    {
        get => _isWombSpreadParasites;
        set
        {
            if (_isWombSpreadParasites == value) return;

            _isWombSpreadParasites = value;
            OnWombSpreadsParasitesChanged?.Invoke(_isWombSpreadParasites);
        }
    }

    public bool IsSpawnSpikeMucus
    {
        get => _isSpawnSpikeMucus;
        set
        {
            if (_isSpawnSpikeMucus == value) return;

            _isSpawnSpikeMucus = value;
            //OnSpawnSpikeMucus?.Invoke(_isSpawnSpikeMucus);
        }
    }

    public void ProtectiveCooconSpawn(bool value) => _isProtectiveCooconSpawn = value;
    public void CocoonSpawnTalent(bool value) => _isCocoonSpawnTalent = value;
    public void ProtectiveCooconSpawnAttack(bool value) => _isProtectiveCooconSpawnAttack = value;
    public void SpawnGetomir(bool value) => IsSpawnGetomir = value;
    public void SpawnSpike(bool value) => _isSpawnSpike = value;
    public void WombSpreadsMucus(bool value) => IsWombSpreadsMucus = value;
    public void WombSpreadsParasites(bool value) => IsWombSpreadsParasites = value;
    public void SpawnSpikeMucus(bool value) => IsSpawnSpikeMucus = value;

    #region Skills Creatures

    private bool _isEffectTentaclesCreatures = false;

    public event Action<bool> OnEffectTentaclesCreatures;

    public bool IsEffectTentaclesCreatures
    {
        get => _isEffectTentaclesCreatures;
        set
        {
            if (_isEffectTentaclesCreatures == value) return;

            _isEffectTentaclesCreatures = value;
            OnEffectTentaclesCreatures?.Invoke(_isEffectTentaclesCreatures);
        }
    }

    public void EffectTentaclesCreatures(bool value) => IsEffectTentaclesCreatures = value;

    #endregion

    #endregion

    protected override IEnumerator PrepareJob(System.Action<TargetInfo> callback)
    {
        Vector3 mousePoint = Targeting.GetMousePoint();

        if (!IsValid(mousePoint))
            yield break;

        _spawnPoint = mousePoint;

        TargetInfo info = new TargetInfo();
        info.Points.Add(_spawnPoint);

        callback(info);
    }

    protected override IEnumerator CastJob()
    {
        if (!IsValid(_spawnPoint)) yield break;

        SpawnWomb(_spawnPoint);

        yield return null;
    }

    private void SpawnWomb(Vector3 position)
    {
        if (_spawnComponent == null || _player == null) return;

        _spawnComponent.CmdSpawnEnemyPoint(position, Quaternion.identity, null, 0, false, _player);

        CmdSpawnWomb();
    }


    [Command]
    private void CmdSpawnWomb() => RpcSpawnWomb();

    [ClientRpc]
    private void RpcSpawnWomb()
    {
        foreach (var womb in _spawnComponent.Units)
        {
            if (womb.TryGetComponent<CreatureSpawn>(out CreatureSpawn creatureSpawn)) creatureSpawn.Tentacle = _tentacle;
            _spawnedWombs.Add(womb.gameObject);
        }
    }

    private bool IsValid(Vector3 v)
    {
        return !(float.IsNaN(v.x) || float.IsInfinity(v.x));
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _spawnPoint = targetInfo.Points[0];
    }

    protected override void ClearData()
    {
        _spawnPoint = Vector3.positiveInfinity;
    }
}