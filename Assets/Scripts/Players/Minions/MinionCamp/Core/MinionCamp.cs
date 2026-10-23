using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.AI;

public enum CampStatus { Neutral, DarkTeam, LightTeam }
public enum CampPhase { Idle, WaitingFirstSpawn, Active, WaitingRespawn }
public enum RespawnManagement { Self, Managed }
public enum CampClearReason { LeaderKilled, LeaderTaken }
public enum SurrenderChoice : byte { TakeCreeps, KeepLeader, Kill }
public enum CampHpDisplay { Never, InCombat, Always }

public partial class MinionCamp : NetworkBehaviour
{
    [Header("Data")]
    [SerializeField] private CampDataSO _data;

    [Header("Spawn zone")]
    [Tooltip("Центр зоны спавна. Если не задан — позиция лагеря")]
    [SerializeField] private Transform _spawnCenter;
    [SerializeField, Min(0f)] private float _spawnRadius = 3f;

    [Header("View")]
    [SerializeField] private MeshRenderer _campRenderer;
    [SerializeField] private CampSurrenderUI _surrenderUI;

    [Header("Rules")]
    [Tooltip("Сколько секунд после последнего действия игрок считается участником зачистки")]
    [SerializeField, Min(0f)] private float _attackerTimeout = 5f;
    [Tooltip("Если никто не выбрал вариант за это время — окно закрывается как «Убить»")]
    [SerializeField, Min(1f)] private float _surrenderOfferTimeout = 15f;
    [Tooltip("InCombat — пока есть участники зачистки или открыто окно захвата")]
    [SerializeField] private CampHpDisplay _hpDisplay = CampHpDisplay.InCombat;

    [SyncVar(hook = nameof(OnOwnerTeamChanged))] private int _ownerTeam = CampTeam.Neutral;
    [SyncVar] private int _level = 1;

    [SyncVar] private bool _hpVisible;
    [SyncVar] private float _hpCurrent;
    [SyncVar] private float _hpMax;
    [SyncVar] private double _campRespawnReadyAt;
    [SyncVar] private float _campRespawnDuration;
    [SyncVar] private int _pendingWaveLevel = 1;
    private readonly SyncList<CreepRespawnInfo> _creepRespawns = new SyncList<CreepRespawnInfo>();

    private readonly CampPopulation _pop = new();
    private readonly CampAttackers _attackers = new();
    private readonly Dictionary<int, List<MinionComponent>> _takenGroups = new();
    private readonly List<MinionComponent> _unitBuffer = new();
    private readonly List<Character> _heroBuffer = new();

    private Character _owner;
    private CampPhase _phase = CampPhase.Idle;
    private float _timer;
    private bool _timerActive;
    private float _surrenderThreshold;
    private float _maxHp;
    private bool _offerActive;
    private bool _offerConsumed;
    private float _offerDeadline;
    private float _hpRefreshTimer;

    partial void TickSurrender(float dt);
    partial void EvaluateSurrender();
    partial void OnLeaderLost();
    partial void ResolveOffer(Character hero, SurrenderChoice choice);

    public event Action<MinionCamp> Spawned;
    public event Action<MinionCamp, CampClearReason> Cleared;

    public int Level => _level;
    public CampPhase Phase => _phase;
    public CampDataSO Data => _data;
    public CampWave CurrentWave => _data.GetWave(_level);
    public float DefaultRespawnDelay => CurrentWave.Leader.RespawnTime;
    public RespawnManagement RespawnMode { get; set; } = RespawnManagement.Self;

    public bool HpVisible => _hpVisible;
    public float HpCurrent => _hpCurrent;
    public float HpMax => _hpMax;
    public IReadOnlyList<CreepRespawnInfo> CreepRespawns => _creepRespawns;

    public bool TryGetCampRespawn(out float remaining, out float duration, out Sprite icon)
    {
        remaining = 0f;
        duration = _campRespawnDuration;
        icon = null;
        if (duration <= 0f || _data == null) return false;

        remaining = Mathf.Max(0f, (float)(_campRespawnReadyAt - NetworkTime.time));
        icon = _data.GetWave(_pendingWaveLevel).Leader.Icon;
        return true;
    }

    public Sprite GetCreepIcon(CreepRespawnInfo info)
    {
        if (_data == null) return null;

        var creeps = _data.GetWave(info.WaveLevel).Creeps;
        return creeps != null && info.CreepIndex >= 0 && info.CreepIndex < creeps.Count
            ? creeps[info.CreepIndex].Unit.Icon
            : null;
    }

    public CampStatus Status => _ownerTeam switch
    {
        CampTeam.DarkTeamIndex => CampStatus.DarkTeam,
        CampTeam.LightTeamIndex => CampStatus.LightTeam,
        _ => CampStatus.Neutral
    };

    public float SurrenderThreshold
    {
        get => _surrenderThreshold;
        set => _surrenderThreshold = Mathf.Clamp01(value);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (_data == null || _data.Waves.Count == 0)
        {
            Debug.LogError("[Minion Camp] Не задан Camp_SO или в нём нет волн", this);
            enabled = false;
            return;
        }

        _surrenderThreshold = _data.SurrenderThreshold;
        _attackers.Configure(_attackerTimeout);

        CampRegistry.Register(this);
        StartCoroutine(AutoStartIfUnmanaged());
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        CampRegistry.Unregister(this);
        _attackers.Clear();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        CampTeam.WhenLocalPlayerReady(this, () => RefreshVisuals(_ownerTeam));
    }

    private IEnumerator AutoStartIfUnmanaged()
    {
        yield return null;
        if (_phase == CampPhase.Idle && !CampRegistry.ManagerPresent)
            Begin();
    }

    [Server]
    public void Begin(float firstSpawnDelay = -1f)
    {
        _level = 1;
        _phase = CampPhase.WaitingFirstSpawn;
        StartCampTimer(firstSpawnDelay >= 0f ? firstSpawnDelay : _data.TimeToFirstSpawn, 1);
    }

    [Server]
    public void ScheduleRespawn(float delay)
    {
        if (_phase != CampPhase.WaitingRespawn) return;
        StartCampTimer(Mathf.Max(0f, delay), _level + 1);
    }

    [Server]
    public void SpawnNow()
    {
        switch (_phase)
        {
            case CampPhase.WaitingFirstSpawn: SpawnWave(false); break;
            case CampPhase.WaitingRespawn: SpawnWave(true); break;
        }
    }

    [Server]
    public void NotifyUnitDamaged(MinionComponent unit, Character attacker)
    {
        RegisterAttacker(attacker);
    }

    private void RegisterAttacker(Character hero)
    {
        if (_ownerTeam != CampTeam.Neutral) return;
        _attackers.Add(hero);
        EvaluateSurrender();
    }

    [Command(requiresAuthority = false)]
    public void CmdChooseSurrender(SurrenderChoice choice, NetworkConnectionToClient sender = null)
    {
        if (!_offerActive) return;

        var hero = _attackers.FindByConnection(sender);
        if (hero == null) return;

        ResolveOffer(hero, choice);
    }

    [ServerCallback]
    private void Update()
    {
        float dt = Time.deltaTime;

        _attackers.Tick(Time.time);
        TickSurrender(dt);
        TickHpDisplay(dt);

        if (!_timerActive) return;

        _timer -= dt;
        if (_timer > 0f) return;

        _timerActive = false;
        if (_phase == CampPhase.WaitingFirstSpawn) SpawnWave(false);
        else if (_phase == CampPhase.WaitingRespawn) SpawnWave(true);
    }
    
    private void SpawnWave(bool levelUp)
    {
        _timerActive = false;
        _campRespawnDuration = 0f;
        if (levelUp) _level++;

        var wave = CurrentWave;

        if (_pop.Leader == null)
            _pop.Leader = SpawnUnit(wave.Leader, _level);

        if (wave.Creeps != null)
        {
            for (int c = 0; c < wave.Creeps.Count; c++)
            {
                var creep = wave.Creeps[c];
                int missing = creep.Count - _pop.CountAlive(creep.Unit.Prefab);
                for (int i = 0; i < missing; i++)
                {
                    var unit = SpawnUnit(creep.Unit, _level);
                    if (unit != null) _pop.AddCreep(creep.Unit, _level, c, unit);
                }
            }
        }

        _maxHp = _pop.TotalMaxHp();
        _offerConsumed = false;
        _phase = CampPhase.Active;
        RefreshHp();

        Spawned?.Invoke(this);
    }

    private MinionComponent SpawnUnit(UnitData data, int level)
    {
        if (data.Prefab == null)
        {
            Debug.LogError("[Minion Camp] В UnitData не задан префаб", this);
            return null;
        }

        var go = Instantiate(data.Prefab, GetSpawnPoint(), Quaternion.identity);
        if (!go.TryGetComponent(out MinionComponent minion))
        {
            Debug.LogError($"[Minion Camp] На префабе {data.Prefab.name} нет MinionComponent", this);
            Destroy(go);
            return null;
        }

        minion.Initialize();
        NetworkServer.Spawn(go);

        minion.SetOwnerTeam(CampTeam.Neutral);
        minion.SetLevel(level);
        minion.SetExpReward(data.XpReward);
        minion.Camp = this;
        minion.Destroyed += OnUnitDestroyed;
        return minion;
    }

    private Vector3 GetSpawnPoint()
    {
        Vector3 center = _spawnCenter != null ? _spawnCenter.position : transform.position;
        Vector2 offset = UnityEngine.Random.insideUnitCircle * _spawnRadius;
        Vector3 candidate = center + new Vector3(offset.x, 0f, offset.y);

        return NavMesh.SamplePosition(candidate, out var hit, 2f, NavMesh.AllAreas)
            ? hit.position
            : center;
    }

    private void OnUnitDestroyed(MinionComponent unit)
    {
        if (!isServer || unit == null) return;
        unit.Destroyed -= OnUnitDestroyed;

        if (unit == _pop.Leader)
        {
            OnLeaderGone();
            return;
        }

        var slot = _pop.FindSlot(unit);
        if (slot == null) return;

        slot.Instance = null;
        if (slot.Controlled && _ownerTeam != CampTeam.Neutral)
        {
            slot.RespawnTimer = slot.Data.RespawnTime;
            TrackCreepRespawn(slot);
        }
        else
        {
            _pop.RemoveSlot(slot);
        }

        RefreshHp();
        EvaluateSurrender();
    }

    private void OnLeaderGone()
    {
        _pop.Leader = null;
        OnLeaderLost();
        EnterWaitingRespawn(CampClearReason.LeaderKilled);
    }

    private void EnterWaitingRespawn(CampClearReason reason)
    {
        _phase = CampPhase.WaitingRespawn;
        _timerActive = false;
        _campRespawnDuration = 0f;

        if (RespawnMode == RespawnManagement.Self)
            StartCampTimer(DefaultRespawnDelay, _level + 1);

        RefreshHp();

        Cleared?.Invoke(this, reason);
    }

    private void StartCampTimer(float delay, int pendingWaveLevel)
    {
        _timer = delay;
        _timerActive = true;
        _pendingWaveLevel = pendingWaveLevel;
        _campRespawnDuration = delay;
        _campRespawnReadyAt = NetworkTime.time + delay;
    }

    private void TrackCreepRespawn(CreepSlot slot)
    {
        _creepRespawns.Add(new CreepRespawnInfo
        {
            SlotId = slot.Id,
            WaveLevel = slot.Level,
            CreepIndex = slot.CreepIndex,
            ReadyAt = NetworkTime.time + slot.Data.RespawnTime,
            Duration = slot.Data.RespawnTime
        });
    }

    private void UntrackCreepRespawn(int slotId)
    {
        for (int i = _creepRespawns.Count - 1; i >= 0; i--)
        {
            if (_creepRespawns[i].SlotId != slotId) continue;
            _creepRespawns.RemoveAt(i);
            return;
        }
    }

    private void TickHpDisplay(float dt)
    {
        _hpRefreshTimer -= dt;
        if (_hpRefreshTimer > 0f) return;

        _hpRefreshTimer = 0.2f;
        RefreshHp();
    }

    private void RefreshHp()
    {
        bool active = _phase == CampPhase.Active && _ownerTeam == CampTeam.Neutral && _pop.Leader != null;

        bool visible = active && _hpDisplay switch
        {
            CampHpDisplay.Never => false,
            CampHpDisplay.Always => true,
            _ => _offerActive || _attackers.Count > 0
        };

        _hpVisible = visible;
        _hpCurrent = active ? _pop.TotalCurrentHp() : 0f;
        _hpMax = active ? _maxHp : 0f;
    }
    
    private void OnOwnerTeamChanged(int oldValue, int newValue) => RefreshVisuals(newValue);

    private void RefreshVisuals(int ownerTeam)
    {
        if (_campRenderer == null) return;

        Color color = Color.gray;
        if (ownerTeam != CampTeam.Neutral && CampTeam.TryGetLocalTeam(out int localTeam))
            color = ownerTeam == localTeam ? Color.blue : Color.red;

        _campRenderer.material.color = color;
    }
}