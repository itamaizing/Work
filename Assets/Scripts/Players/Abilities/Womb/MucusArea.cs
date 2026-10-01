using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class MucusArea : NetworkBehaviour
{
    [SerializeField] private float _growSpeed = 1f;
    [SerializeField] private float _maxRadius = 6f;
    [SerializeField] private float _tick = 0.1f;
    [SerializeField] private float _spikeInterval = 2f;
    [SerializeField] private float _spikeDamage = 10f;
    [SerializeField] private float _parasiteInterval = 3f;
    [SerializeField] private float _slimeStackInterval = 1f;
    [SerializeField] private LayerMask _characterMask;

    [SyncVar] private WombSpawn _womb;
    [SyncVar] private bool _active;
    [SyncVar] private double _startTime;
    [SyncVar] private double _stopTime = -1;

    public WombSpawn Womb => _womb;
    public float SpikeInterval => _spikeInterval;
    public float MaxRadius => _maxRadius;
    public double StartTime => _startTime;
    public bool IsActive => _active;
    public bool SpikesActive => _womb != null && _womb.IsSpawnSpikeMucus;
    public float Radius
    {
        get
        {
            if (!_active) return 0f;

            double now = NetworkTime.time;
            double growEnd = _stopTime < 0 ? now : _stopTime;
            float peak = Mathf.Min(_maxRadius, (float)(growEnd - _startTime) * _growSpeed);

            if (_stopTime < 0) return peak;
            return Mathf.Max(0f, peak - (float)(now - _stopTime) * _growSpeed);
        }
    }
    
    public HashSet<Character> InZoneEnemies
    {
        get
        {
            HashSet<Character> enemies = new HashSet<Character>();
            foreach (Character c in _inZone)
            {
                if (c != null && IsEnemy(c))
                {
                    enemies.Add(c);
                }
            }
            return enemies;
        }
    }
    
    private readonly Collider[] _buffer = new Collider[64];
    private readonly HashSet<Character> _inZone = new();
    private readonly HashSet<Character> _current = new();
    private readonly Dictionary<Character, float> _nextParasite = new();
    private float _nextSpike;

    [Server] public void ServerBind(WombSpawn womb) => _womb = womb;

    [Server]
    public void BeginShrink()
    {
        if (_active && _stopTime < 0) _stopTime = NetworkTime.time;
    }

    public override void OnStartServer() => InvokeRepeating(nameof(ServerTick), 0.1f, _tick);

    public override void OnStopServer()
    {
        CancelInvoke();
        ClearAll();
    }

    [ServerCallback]
    private void ServerTick()
    {
        if (_womb == null) return;

        bool wantActive = _womb.IsWombSpreadsMucus;
        if (wantActive != _active)
        {
            _active = wantActive;
            _startTime = NetworkTime.time;
            _stopTime = -1;
            if (!_active) ClearAll();
        }

        if (!_active) return;

        int count = Physics.OverlapSphereNonAlloc(transform.position, Radius, _buffer, _characterMask);

        _current.Clear();
        for (int i = 0; i < count; i++)
            if (_buffer[i].TryGetComponent(out Character c))
                _current.Add(c);

        foreach (Character c in _current)
            if (_inZone.Add(c))
                OnEnterZone(c);

        _inZone.RemoveWhere(c =>
        {
            if (c == null) return true;
            if (_current.Contains(c)) return false;

            OnExitZone(c);
            _nextParasite.Remove(c);
            return true;
        });

        float now = Time.time;

        foreach (Character c in _inZone)
        {
            if (IsPsiAlly(c)) TickSlime(c, now);
            if (_womb.IsWombSpreadsParasites && IsEnemy(c)) TickParasites(c, now);
        }

        if (_womb.IsSpawnSpikeMucus && now >= _nextSpike)
        {
            _nextSpike = now + _spikeInterval;

            List<GameObject> targetsToSpike = new List<GameObject>();

            foreach (Character c in _inZone)
            {
                if (!IsEnemy(c)) continue;

                _womb.ApplyDamage(
                    new Damage { Value = _spikeDamage, Type = DamageType.Physical, School = Schools.Air },
                    c.gameObject);

                targetsToSpike.Add(c.gameObject);
            }

            if (targetsToSpike.Count > 0)
            {
                RpcSpawnSpikesVisual(targetsToSpike.ToArray());
            }
        }
    }

    public event System.Action<GameObject[]> OnSpikePulseTriggered;
    
    [ClientRpc]
    private void RpcSpawnSpikesVisual(GameObject[] targets)
    {
            OnSpikePulseTriggered?.Invoke(targets);
    }
    
    private void OnEnterZone(Character c)
    {
        if (!IsPsiAlly(c) || !c.TryGetComponent(out CharacterState state)) return;

        if (state.GetState(States.HealingSlime) is HealingSlime existing)
        {
            existing.SwitchToInfinite();
            RpcSlimeMode(c.gameObject, true);
            return;
        }

        state.AddState(States.HealingSlime, 9999f, 0f, gameObject, name);

        if (state.GetState(States.HealingSlime) is HealingSlime created)
            created.NextStackDueTime = Time.time + _slimeStackInterval;
    }

    private void OnExitZone(Character c)
    {
        if (c.TryGetComponent(out CharacterState state) &&
            state.GetState(States.HealingSlime) is HealingSlime slime)
        {
            slime.SwitchToFinite();
            RpcSlimeMode(c.gameObject, false);
        }
    }

    private void TickSlime(Character c, float now)
    {
        if (!c.TryGetComponent(out CharacterState state) ||
            state.GetState(States.HealingSlime) is not HealingSlime slime) return;

        if (now < slime.NextStackDueTime) return;
        slime.NextStackDueTime = now + _slimeStackInterval;

        if (slime.CurrentStacksCount < slime.MaxStacksCount)
            state.AddState(States.HealingSlime, 9999f, 0f, gameObject, name);
    }

    private void TickParasites(Character c, float now)
    {
        if (_nextParasite.TryGetValue(c, out float next) && now < next) return;
        _nextParasite[c] = now + _parasiteInterval;

        if (c.TryGetComponent(out CharacterState state))
            state.AddState(States.Parasites, 12f, 1000f, gameObject, name);
    }

    [ClientRpc]
    private void RpcSlimeMode(GameObject target, bool infinite)
    {
        if (isServer || target == null) return;
        if (!target.TryGetComponent(out CharacterState state)) return;
        if (state.GetState(States.HealingSlime) is not HealingSlime slime) return;

        if (infinite) slime.SwitchToInfinite();
        else slime.SwitchToFinite();
    }

    private bool IsPsiAlly(Character c) =>
        c.GetComponent<PsionicEnergySkill>() != null ||
        (c.CharacterParent != null && c.CharacterParent.GetComponent<PsionicEnergySkill>() != null);

    private bool IsEnemy(Character c) =>
        c.NetworkSettings != null &&
        c.NetworkSettings.TeamIndex != _womb.Hero.NetworkSettings.TeamIndex;

    private void ClearAll()
    {
        foreach (Character c in _inZone)
            if (c != null) OnExitZone(c);

        _inZone.Clear();
        _nextParasite.Clear();
    }
}
