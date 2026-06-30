using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class IcyStream : Skill, IEnergyDamagable,IComboSeriesParticipatingSkill
{
    public struct IcyStreamState
    {
        public int CurrentTick;
        public int MaxTicks;
        public Vector3 Direction;
        public Vector3 StreamOrigin;
    }

    [Header("Stream Settings")]
    [SerializeField] private float _tickInterval = 0.3f;
    [SerializeField] private float _streamWidth = 1f;
    [SerializeField] private float _streamLength = 4f;

    [Header("Visual")]
    [SerializeField] private GameObject _icyStreamPrefab;

    private float _runeCost = 1f;
    private float _energyPerTick = 5f;
    private float _maxEnergySpend = 40f;
    private float _energySpent;

    private float _freeWindowDuration = 0.6f;
    private Coroutine _streamCoroutine;
    private GameObject _activeEffect;

    private bool _isStreaming;
    private int _currentTick;
    private const float BaseStreamWidth = 1f;
    private const float FrostEnergyCoolingBonusPerStack = 1f;
    private const float MaxDistanceRayCast = 100f;
    private const float MinRotationThresholdSqr = 0.01f;

    public IcyStreamState CurrentState { get; private set; }
    public bool IsStreamSkill => true;
    public bool IsFrostEnergyApplied => true;

    protected override bool IsCanCast =>
        !_isStreaming && HasEnoughResourcesToStart();
    
    private int FreeTicks => Mathf.RoundToInt(_freeWindowDuration / _tickInterval);
    
    private int MaxPaidTicks => Mathf.FloorToInt(_maxEnergySpend / _energyPerTick);
    
    private int MaxTicks => FreeTicks + MaxPaidTicks;

    private bool HasEnoughResourcesToStart()
    {
        var energy = Hero.Resources[ResourceType.Energy];
        var rune   = Hero.Resources[ResourceType.Rune];
        return energy.CurrentValue >= _energyPerTick && rune.CurrentValue >= _runeCost;
    }

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    private void OnEnable()  => OnSkillCanceled += HandleCancel;
    private void OnDisable() => OnSkillCanceled -= HandleCancel;

    private void HandleCancel() => StopStream();

    public void StopStream()
    {
        if (_streamCoroutine != null)
        {
            StopCoroutine(_streamCoroutine);
            _streamCoroutine = null;
        }

        CmdDestroyIcyStreamEffect();
        CmdResetEnergyMultiplier();
        _isStreaming = false;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (!GetMouseButton)
            yield return null;

        callbackDataSaved(new TargetInfo());
    }

    protected override IEnumerator CastJob()
    {
        if (!Cost.TryPaySingle(_runeCost, ResourceType.Rune, shouldModify: false))
        {
            TryCancel(true);
            yield break;
        }

        _isStreaming = true;
        _energySpent = 0f;
        _currentTick = 0;

        _streamCoroutine = StartCoroutine(StreamRoutine());
        CmdSpawnIcyStreamEffect(_isFinalHit);
        yield return _streamCoroutine;

        CmdDestroyIcyStreamEffect();
        CmdResetEnergyMultiplier();
        _isStreaming = false;
        _isFinalHit = false;
        _isTicking = false;
    }

    private IEnumerator StreamRoutine()
    {
        int freeTicks = FreeTicks;
        int maxTicks = MaxTicks;

        if (_isFinalHit)
        {
            _streamWidth = BaseStreamWidth * 2f;
        }
        else
        {
            _streamWidth = BaseStreamWidth;
        }

        _isTicking = true;
        
        for (int tick = 1; tick <= maxTicks; tick++)
        {
            yield return new WaitForSeconds(_tickInterval);

            bool isFreeTick = tick <= freeTicks;

            if (!isFreeTick)
            {
                if (!Cost.TryPaySingle(_energyPerTick, ResourceType.Energy, shouldModify: false))
                {
                    TryCancel(true);
                    yield break;
                }

                _energySpent += _energyPerTick;
            }

            _currentTick = tick;
            if (_currentTick == maxTicks)
            {
                _isTicking = false;
            }
            
            OnSeriesDamaged?.Invoke(null,this);

            CurrentState = new IcyStreamState
            {
                CurrentTick = tick,
                MaxTicks = maxTicks,
                Direction = transform.forward,
                StreamOrigin = transform.position
            };

            ApplyTick(tick);
        }
    }

    private void ApplyTick(int tickNumber)
    {
        Vector3 start = transform.position;
        Vector3 end = transform.position + transform.forward * _streamLength;

        Collider[] hits = Physics.OverlapCapsule(start, end, _streamWidth * 0.5f, _targetsLayers);

        foreach (var col in hits)
        {
            if ((_targetsLayers.value & (1 << col.gameObject.layer)) == 0) continue;
            if (!col.TryGetComponent<Character>(out var target)) continue;
            if (target.IsDead) continue;

            Damage damage = new Damage
            {
                Value = tickNumber,
                Type = Info.DamageType,
                School = Schools.Water
            };

            CmdApplyDamage(damage, target.gameObject);
            CmdAddCooling(target);
        }
    }

    [Command]
    private void CmdAddCooling(Character character)
    {
        if (character == null) return;
        ApplyCoolingWithFrostEnergyBonus(character);
    }

    private void ApplyCoolingWithFrostEnergyBonus(Character target)
    {
        target.CharacterState.AddState(States.Cooling, 12f, 0, Hero.gameObject, Name);

        _hero.Abilities.GetSkill<FrostEnergy>()?.ApplyFrostEnergyStateBonus(target, States.Cooling, this);
    }

    private void PayRemainingEnergy()
    {
        if (!_isStreaming) return;
        if (_currentTick >= MaxTicks) return;

        int remaining = MaxTicks - _currentTick;
        float totalEnergyLeft = remaining * _energyPerTick;

        if (Hero.TryGetResource(ResourceType.Energy, out var resource))
            resource.CmdUse(totalEnergyLeft);
    }

    [Command]
    private void CmdSpawnIcyStreamEffect(bool isFinalHit)
    {
        if (_icyStreamPrefab == null) return;

        GameObject fx = Instantiate(
            _icyStreamPrefab,
            transform.position,
            Quaternion.identity);

        fx.transform.SetParent(transform);
        fx.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        NetworkServer.Spawn(fx, connectionToClient);

        _activeEffect = fx;
        
        if (isFinalHit)
        {
            var ps = fx.GetComponentInChildren<ParticleSystem>();
            var main = ps.main;
            main.startSize = main.startSize.constant * 2;
            
            SetParticleSizeOnClients(fx);
        }

        RpcStartFollowMouse(fx);
    }

    [ClientRpc]
    private void SetParticleSizeOnClients(GameObject particleObject)
    {
        if(particleObject == null) return;
        var ps = particleObject.GetComponentInChildren<ParticleSystem>();
        var main = ps.main;
        main.startSize = main.startSize.constant * 2;
    }

    [ClientRpc]
    private void RpcStartFollowMouse(GameObject fx)
    {
        if (isOwned)
            StartCoroutine(FollowMouseRoutine(fx));
    }

    private IEnumerator FollowMouseRoutine(GameObject fx)
    {
        while (fx != null && _isStreaming)
        {
            Vector3 mousePos  = GetMouseWorldPosition();
            Vector3 direction = mousePos - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > MinRotationThresholdSqr)
            {
                Quaternion rot = Quaternion.LookRotation(direction, Vector3.up);
                CmdRotateEffects(rot);
            }

            yield return null;
        }
    }

    [Command]
    private void CmdRotateEffects(Quaternion rotation)
    {
        if (_activeEffect != null)
            _activeEffect.transform.rotation = rotation;
    }

    [Command]
    private void CmdDestroyIcyStreamEffect()
    {
        if (_activeEffect != null)
        {
            NetworkServer.Destroy(_activeEffect);
            _activeEffect = null;
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, MaxDistanceRayCast))
            return hit.point;
        return transform.position + transform.forward * _streamLength;
    }

    public bool TryGetState(out IcyStreamState state)
    {
        if (!_isStreaming) { state = default; return false; }
        state = CurrentState;
        return true;
    }

    [Command]
    private void CmdResetEnergyMultiplier()
    {
        _hero.Abilities.GetSkill<NinjaResources>()?.ResetMultiplierIfOwner(this);
    }

    public override void LoadTargetData(TargetInfo targetInfo) { }

    protected override void ClearData()
    {
        if (_streamCoroutine != null)
        {
            StopCoroutine(_streamCoroutine);
            _streamCoroutine = null;
        }
    }

    #region Series

    private bool _isFinalHit;
    private bool _isTicking;
    public event IComboSeriesParticipatingSkill.OnBeforeApplyDamageDelegate OnBeforeApplySeriesDamage;
    public event Action<GameObject, Skill> OnSeriesDamaged;
    public float EnergyCostOnHit => _energyPerTick;
    public float RuneCostOnHit => _runeCost;
    public bool IsTicking => _isTicking;

    public void OnSeriesHit(int hitCountInCurrentSeries, Character target)
    {
    }

    public void OnSeriesCompleted(Character target, int totalHits, float totalEnergySpent)
    {
        _isFinalHit = true;
    }

    public void OnSeriesBroken(Character target)
    {
    }

    public void OnSeriesPotentialFinal(Skill skill, bool isPotentialFinal)
    {
        _isFinalHit = isPotentialFinal;
    }

    #endregion

}