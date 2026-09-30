using System.Collections;
using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System;

[Flags]
public enum WombFlags : byte
{
    None      = 0,
    Mucus     = 1 << 0,
    Parasites = 1 << 1,
    Spikes    = 1 << 2,
    Getomir   = 1 << 3,
    Tentacles = 1 << 4,
}

public class WombSpawn : Skill
{
    [SerializeField] private Character _player;
    [SerializeField] private SpawnComponent _spawnComponent;
    [SerializeField] private SummoningSwarm _summoningSwarm;
    [SerializeField] private float _radiusTarget = 0.5f;

    private bool _isClickedOnGround = false;

    private Vector3 _spawnPoint = Vector3.positiveInfinity;
    private readonly List<GameObject> _spawnedWombs = new();

    #region Talent

    [SyncVar(hook = nameof(OnFlagsChanged))] private WombFlags _flags;

    public event Action<bool> OnSpawnGetomirChanged;
    public event Action<bool> OnWombSpreadsMucusChanged;
    public event Action<bool> OnWombSpreadsParasitesChanged;
    public event Action<bool> OnSpawnSpikeMucusChanged;
    public event Action<bool> OnEffectTentaclesCreatures;

    public bool Has(WombFlags f) => (_flags & f) != 0;

    public bool IsSpawnGetomir => Has(WombFlags.Getomir);
    public bool IsWombSpreadsMucus => Has(WombFlags.Mucus);
    public bool IsWombSpreadsParasites => Has(WombFlags.Parasites);
    public bool IsSpawnSpikeMucus => Has(WombFlags.Spikes);
    public bool IsEffectTentaclesCreatures => Has(WombFlags.Tentacles);
    
    public void SpawnGetomir(bool value) => SetFlag(WombFlags.Getomir, value);
    public void WombSpreadsMucus(bool value) => SetFlag(WombFlags.Mucus, value);
    public void WombSpreadsParasites(bool value) => SetFlag(WombFlags.Parasites, value);
    public void SpawnSpikeMucus(bool value) => SetFlag(WombFlags.Spikes, value);
    public void EffectTentaclesCreatures(bool value) => SetFlag(WombFlags.Tentacles, value);

    private void SetFlag(WombFlags flag, bool on)
    {
        if (isServer) ApplyFlag(flag, on);
        else CmdSetFlag(flag, on);
    }

    [Command]
    private void CmdSetFlag(WombFlags flag, bool on) => ApplyFlag(flag, on);

    [Server]
    private void ApplyFlag(WombFlags flag, bool on) =>
        _flags = on ? _flags | flag : _flags & ~flag;

    private void OnFlagsChanged(WombFlags oldValue, WombFlags newValue)
    {
        if (!isOwned) return;

        WombFlags diff = oldValue ^ newValue;
        if ((diff & WombFlags.Getomir) != 0) OnSpawnGetomirChanged?.Invoke((newValue & WombFlags.Getomir) != 0);
        if ((diff & WombFlags.Mucus) != 0) OnWombSpreadsMucusChanged?.Invoke((newValue & WombFlags.Mucus) != 0);
        if ((diff & WombFlags.Parasites) != 0) OnWombSpreadsParasitesChanged?.Invoke((newValue & WombFlags.Parasites) != 0);
        if ((diff & WombFlags.Spikes) != 0) OnSpawnSpikeMucusChanged?.Invoke((newValue & WombFlags.Spikes) != 0);
        if ((diff & WombFlags.Tentacles) != 0) OnEffectTentaclesCreatures?.Invoke((newValue & WombFlags.Tentacles) != 0);
    }

    #endregion

    private LayerMask _alliesMask;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("Spell");

    private bool IsValidVector(Vector3 vector)
    {
        return !(float.IsNaN(vector.x) || float.IsNaN(vector.y) || float.IsNaN(vector.z) ||
                 float.IsInfinity(vector.x) || float.IsInfinity(vector.y) || float.IsInfinity(vector.z));
    }

    private void OnDisable()
    {
        OnSkillCanceled -= HandleSkillCanceled;
    }

    private void OnEnable()
    {
        OnSkillCanceled += HandleSkillCanceled;
    }

    private void Start()
    {
        _alliesMask = LayerMask.GetMask("Allies");
    }

    private void HandleSkillCanceled()
    {
        Targeting.ClearTarget();
        ClearData();
    }

    public void MoveStop()
    {
        Hero.Move.SetCanMove(false);
        if (Targeting.GetTarget() != null) _player.Move.LookAtPosition(Targeting.GetTarget().Position);
        Hero.Move.StopMoveAndAnimationMove();
    }

    public void AnimTentaclesCast()
    {
        if (isClient)
            CommitUse();
        AnimStartCastCoroutine();
    }

    public void AnimTentaclesCastEnd()
    {
        AnimCastEnded();
    }

    protected override void ClearData()
    {
        _skillRender.IsOverrideClosestTarget = false;
        _isClickedOnGround = false;
        _skillRender.StopDrawRadius();
        _isPlayCastAnim = false;

        _spawnPoint = Vector3.positiveInfinity;
        Targeting.ClearTarget();
        Hero.Move.SetCanMove(true);
        _player.Move.StopLookAt();
    }

    protected override IEnumerator CastJob()
    {
        _spawnPoint = Targeting.GetTarget().Position;
        if (!IsValidVector(_spawnPoint)) yield break;

        bool hadCharges = _summoningSwarm != null && _summoningSwarm.ChargesSwarm > 0;

        if (hadCharges) _summoningSwarm.UseSwarmCharges(1);

        SpawnWomb(_spawnPoint);

        if (hadCharges) Cooldown.ForceEnd();

        ClearData();
        yield return null;
    }

    public void SpawnWombExternal(Vector3 pos) => SpawnWomb(pos);

    private void SpawnWomb(Vector3 position)
    {
        if (!IsValidVector(position)) return;
        CmdSpawnWombAndAssign(position, Hero);
    }

    [Command]
    private void CmdSpawnWombAndAssign(Vector3 position, Character parentCharacter)
    {
        var spawned = _spawnComponent.SpawnAliesPointServer(position, Quaternion.identity, null, 3, false, parentCharacter);
        _skillRender.StopDrawRadius();

        if (spawned == null) return;

        var zone = spawned.GetComponentInChildren<MucusArea>(true);
        if (zone != null) zone.ServerBind(this);

        BindWomb(spawned.gameObject);
        RpcTentacleWomb(spawned.netIdentity);
    }

    [ClientRpc]
    private void RpcTentacleWomb(NetworkIdentity wombIdentity)
    {
        if (wombIdentity == null) return;

        BindWomb(wombIdentity.gameObject);

        if (!_spawnedWombs.Contains(wombIdentity.gameObject))
            _spawnedWombs.Add(wombIdentity.gameObject);
    }

    private void BindWomb(GameObject wombObject)
    {
        foreach (var creatureSpawn in wombObject.GetComponentsInChildren<CreatureSpawn>(true))
            creatureSpawn.WombSpawn = this;
    }
}