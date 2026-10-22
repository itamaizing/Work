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

[DisallowMultipleComponent]
public partial class MinionCamp : NetworkBehaviour
{
    [Header("Data")]
    [SerializeField] private CampDataSO _data;

    [Header("Spawn zone")]
    [SerializeField] private Transform _spawnCenter;
    [SerializeField, Min(0f)] private float _spawnRadius = 3f;

    [Header("View")]
    [SerializeField] private MeshRenderer _campRenderer;
    [SerializeField] private CampSurrenderUI _surrenderUI;

    [Header("Rules")]
    [SerializeField, Min(0f)] private float _attackerTimeout = 5f;
    [SerializeField, Min(1f)] private float _surrenderOfferTimeout = 15f;

    [SyncVar(hook = nameof(OnOwnerTeamChanged))] private int _ownerTeam = CampTeam.Neutral;
    [SyncVar] private int _level = 1;
    
    private readonly CampPopulation _pop = new();
    private readonly CampAttackers _attackers = new();
    private readonly Dictionary<int, List<CampMinionComponent>> _takenGroups = new();
    private readonly List<CampMinionComponent> _unitBuffer = new();
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
        _timer = firstSpawnDelay >= 0f ? firstSpawnDelay : _data.TimeToFirstSpawn;
        _timerActive = true;
    }

    [Server]
    public void ScheduleRespawn(float delay)
    {
        if (_phase != CampPhase.WaitingRespawn) return;
        _timer = Mathf.Max(0f, delay);
        _timerActive = true;
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
    public void AddAttacker(GameObject attacker)
    {
        if (attacker != null && attacker.TryGetComponent(out Character hero))
            RegisterAttacker(hero);
    }

    [Server]
    public void NotifyUnitDamaged(CampMinionComponent unit, Character attacker)
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
        if (levelUp) _level++;

        var wave = CurrentWave;

        if (_pop.Leader == null)
            _pop.Leader = SpawnUnit(wave.Leader, _level);

        if (wave.Creeps != null)
        {
            foreach (var creep in wave.Creeps)
            {
                int missing = creep.Count - _pop.CountAlive(creep.Unit.Prefab);
                for (int i = 0; i < missing; i++)
                {
                    var unit = SpawnUnit(creep.Unit, _level);
                    if (unit != null) _pop.AddCreep(creep.Unit, _level, unit);
                }
            }
        }

        _maxHp = _pop.TotalMaxHp();
        _offerConsumed = false;
        _phase = CampPhase.Active;

        Spawned?.Invoke(this);
    }

    private CampMinionComponent SpawnUnit(UnitData data, int level)
    {
        if (data.Prefab == null)
        {
            Debug.LogError("[Minion Camp] В UnitData не задан префаб", this);
            return null;
        }

        var go = Instantiate(data.Prefab, GetSpawnPoint(), Quaternion.identity);
        if (!go.TryGetComponent(out CampMinionComponent minion))
        {
            Debug.LogError($"[Minion Camp] На префабе {data.Prefab.name} нет CampMinionComponent", this);
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
    
    private void OnUnitDestroyed(CampMinionComponent unit)
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
            slot.RespawnTimer = slot.Data.RespawnTime;
        else
            _pop.RemoveSlot(slot);
        
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

        if (RespawnMode == RespawnManagement.Self)
        {
            _timer = DefaultRespawnDelay;
            _timerActive = true;
        }

        Cleared?.Invoke(this, reason);
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
    
    public void SetPlayers(GameObject playersObject) { }
}
