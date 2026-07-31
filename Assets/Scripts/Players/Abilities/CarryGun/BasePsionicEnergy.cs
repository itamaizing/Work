using Mirror;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BasePsionicEnergy : Resource, IDamageable
{
    [SerializeField] protected Character _heroCharacter;
    [SerializeField] private AttackingPsionicEnergy _attackingPsionicEnergy;
    [SerializeField] private Slider basePsionicsSlider;
    [SerializeField] private PsionicEnergySkill psionicEnergySkill;
    [SerializeField] private float _psionicaDecayTime = 12f;

    private const float BasePsionicaThreshold = 30f;
    private const float BaseSliderFillPercent = 0.3f;
    private const float RemainingSliderFillPercent = 0.7f;
    private const float DamageToPsiConversionRate = 0.2f;
    private const float DistanceStep = 1f;

    private const float PsiDissipationPercent = 0.3f;
    private const float PsiDissipationRadius = 3f;

    private bool _isDissipatingPsi = false;
    
    private Vector3 _lastPosition;
    private float _distanceAccumulator;
    private bool _isInternalPsiEnergy = false;
    private bool _isAccumulationPsionicRunning = false;
    private bool _isTakesAnyDamage = false;
    private Coroutine _energyDecayCoroutine;
    private bool _isInitialized = false;

    private float MaxPsi => _heroCharacter.Health.MaxValue;
    public bool IsAttackingPsiEnergyActive => _attackingPsionicEnergy.IsAttackingPsiEnergy;
    
    public static float PsiPerMeter => 1f;
    
    public event Action<Damage, Skill> DamageTaken;
    public event Action<float> OnEnergyChanged;
    public event Action<bool> OnAccumulationPsionicChanged;

    public PsionicEnergySkill PsionicEnergySkill { get => psionicEnergySkill; set => psionicEnergySkill = value; }
    public float PsionicaDecayTime { get => _psionicaDecayTime; set => _psionicaDecayTime = value; }

    private void Start()
    {
        InitializePsionicResource();
    }

    private void InitializePsionicResource()
    {
        if (_isInitialized) return;
        if (_heroCharacter == null || _heroCharacter.Health == null) return;

        _maxValue = _heroCharacter.Health.MaxValue;
        CurrentValue = 0f;

        if (!_heroCharacter.Health.Shields.Contains(this))
            _heroCharacter.Health.Shields.Add(this);

        UpdatePsionicaBar();

        _isInitialized = true;
    }

    public override void Initialize(Attribute maxValue, Attribute regenValue, CharacterData data)
    {
        base.Initialize(maxValue, regenValue, data);
    }

    public void TakesAnyDamage(bool value) => _isTakesAnyDamage = value;
    public void AccumulationPsionicChanged(bool value) => OnAccumulationPsionicChanged?.Invoke(value);

    public void DissipatingPsi(bool value)
    {
        if(value == _isDissipatingPsi) return;
        _isDissipatingPsi = value;
    }
    public void AccumulationPsionicRunning(bool value)
    {
        _isAccumulationPsionicRunning = value;
        _lastPosition = _heroCharacter.transform.position;
        _distanceAccumulator = 0f;
    }

    public override void Init(ResourceAttribute resource)
    {
        base.Init(resource);

        if (_heroCharacter != null)
        {
            _maxValue = _heroCharacter.AttributeSystem.HPMax.GetValue();

            if (!_heroCharacter.Health.Shields.Contains(this))
                _heroCharacter.Health.Shields.Add(this);
        }

        CurrentValue = 0f;

        if (isServer)
            RpcOnEnergyChanged(CurrentValue);

        UpdatePsionicaBar();

        _isInitialized = true;
    }

    private void Update()
    {
        UpdatePsionicaBar();
        PsionicRunning();
    }

    private void OnEnable()
    {
        if (_heroCharacter.DamageTracker != null)
        {
            _heroCharacter.DamageTracker.OnDamageTracked += OnDamageDealt;
        }

        if (_heroCharacter.SpawnComponent != null)
        {
            _heroCharacter.SpawnComponent.UnitAdded += OnMinionSpawned;
            _heroCharacter.SpawnComponent.UnitRemoved += OnMinionRemoved;
        }
        
        if (_heroCharacter.Health != null)
        {
            _heroCharacter.Health.MaxValueChanged += OnHealthMaxValueChanged;
        }

        _heroCharacter.Reset += PsiEnergyDecayServer;
    }

    private void OnDisable()
    {
        if (_heroCharacter != null && _heroCharacter.DamageTracker != null) _heroCharacter.DamageTracker.OnDamageTracked -= OnDamageDealt;

        if (_heroCharacter.SpawnComponent != null)
        {
            _heroCharacter.SpawnComponent.UnitAdded -= OnMinionSpawned;
            _heroCharacter.SpawnComponent.UnitRemoved -= OnMinionRemoved;

            foreach (var unit in _heroCharacter.SpawnComponent.Units)
            {
                if (unit != null && unit.DamageTracker != null) unit.DamageTracker.OnDamageTracked -= OnDamageDealt;
            }
        }

        if (_heroCharacter.Health != null)
        {
            _heroCharacter.Health.MaxValueChanged -= OnHealthMaxValueChanged;
        }

        _heroCharacter.Reset -= PsiEnergyDecayServer;
    }

    private void OnHealthMaxValueChanged(float oldMax, float newMax)
    {
        _maxValue = newMax;

        if (CurrentValue > _maxValue)
        {
            CurrentValue = _maxValue;
            RpcOnEnergyChanged(CurrentValue);
        }

        UpdatePsionicaBar();
    }
    
    public void AddPsiByDistance(float distance)
    {
        if (distance <= 0f) return;
        AddPsiAndRestartDecay(distance * PsiPerMeter);
    }

    private void PsionicRunning()
    {
        if (!_isAccumulationPsionicRunning) return;
        if (!_attackingPsionicEnergy.IsAttackingPsiEnergy) return;

        Vector3 currentPos = _heroCharacter.transform.position;
        float distanceDelta = Vector3.Distance(currentPos, _lastPosition);
        if (distanceDelta <= 0.001f) return;

        AddPsiByDistance(distanceDelta);

        _lastPosition = currentPos;
    }

    private void OnMinionSpawned(Character minion)
    {
        if (minion == null || minion.DamageTracker == null) return;

        minion.DamageTracker.OnDamageTracked += OnDamageDealt;
    }
    private void OnMinionRemoved(Character minion)
    {
        if (minion == null || minion.DamageTracker == null) return;

        minion.DamageTracker.OnDamageTracked -= OnDamageDealt;
    }

    private void OnDamageDealt(Damage damage, GameObject target)
    {
        if (!_isTakesAnyDamage && damage.Type != DamageType.Physical) return;
        if (psionicEnergySkill == null || !psionicEnergySkill.IsPsiEnergyActive) return;

        float energyGain = damage.Value * DamageToPsiConversionRate;
        CurrentValue = Mathf.Min(CurrentValue + energyGain, MaxPsi);

        RpcCoolDownPsionicEnegry();
        RpcOnEnergyChanged(CurrentValue);

        bool wasInternalEnergy = _isInternalPsiEnergy;
        _isInternalPsiEnergy = CurrentValue > 0;

        if (wasInternalEnergy != _isInternalPsiEnergy)
            RpcInternalPsiEnergyChanged(_isInternalPsiEnergy);

        if (_energyDecayCoroutine != null)
            StopCoroutine(_energyDecayCoroutine);

        _energyDecayCoroutine = StartCoroutine(EnergyDecayCoroutine());

        UpdatePsionicaBar();
    }

    public void AddPsiAndRestartDecay(float value)
    {
        if (!isServer) return;

        if (psionicEnergySkill == null || !psionicEnergySkill.IsPsiEnergyActive)
            return;

        CurrentValue = Mathf.Min(CurrentValue + value, MaxPsi);

        RpcOnEnergyChanged(CurrentValue);

        bool wasInternalEnergy = _isInternalPsiEnergy;
        _isInternalPsiEnergy = CurrentValue > 0;

        if (wasInternalEnergy != _isInternalPsiEnergy)
            RpcInternalPsiEnergyChanged(_isInternalPsiEnergy);

        if (_energyDecayCoroutine != null)
            StopCoroutine(_energyDecayCoroutine);

        _energyDecayCoroutine = StartCoroutine(EnergyDecayCoroutine());

        RpcCoolDownPsionicEnegry();
    }

    [ClientRpc] public void RpcCoolDownPsionicEnegry() => CoolDownPsionicEnegry();

    //public void CoolDownPsionicEnegry() => psionicEnergySkill.IncreaseSetCooldownPassive(_psionicaDecayTime);
    public void CoolDownPsionicEnegry() => psionicEnergySkill.Cooldown.SetIncreased(_psionicaDecayTime, shouldModify: true);

    public void UsePsiEnergy(float value)
    {
        TryUse(value);
        RpcOnEnergyChanged(CurrentValue);
        UpdatePsionicaBar();
    }

    private void UpdatePsionicaBar()
    {
        if (basePsionicsSlider == null || MaxPsi <= 0f) return;

        float normalizedValue = 0f;

        if (CurrentValue <= BasePsionicaThreshold)
        {
            normalizedValue = (CurrentValue / BasePsionicaThreshold) * BaseSliderFillPercent;
        }
        else
        {
            float remainingValue = (CurrentValue - BasePsionicaThreshold) / (MaxPsi - BasePsionicaThreshold);
            normalizedValue = BaseSliderFillPercent + (remainingValue * RemainingSliderFillPercent);
        }

        basePsionicsSlider.value = normalizedValue;
    }

    private IEnumerator EnergyDecayCoroutine()
    {
        yield return new WaitForSeconds(_psionicaDecayTime);
        PsiEnergyDecay();
    }

    public void PsiEnergyDecayServer()
    {
        if (!isServer) return;
        PsiEnergyDecay();
    }

    private void PsiEnergyDecay()
    {
        CurrentValue = 0;
        RpcOnEnergyChanged(CurrentValue);
        _isInternalPsiEnergy = false;
        UpdatePsionicaBar();
        RpcInternalPsiEnergyChanged(_isInternalPsiEnergy);
    }

    public bool TryTakeDamage(ref Damage damage, Skill skill)
    {
        if (damage.Value == 0) return true;

        if (CurrentValue > 0)
        {
            float absorbingDamage = Mathf.Min(CurrentValue, damage.Value);
            damage.Value -= absorbingDamage * 0.5f;
            UsePsiEnergy(absorbingDamage);

            _isInternalPsiEnergy = CurrentValue > 0;
            RpcInternalPsiEnergyChanged(_isInternalPsiEnergy);
            UpdatePsionicaBar();

            if (_isDissipatingPsi) DissipatePsiDamage(absorbingDamage, skill);

            return true;
        }

        return false;
    }

    private void DissipatePsiDamage(float absorbedAmount, Skill skill)
    {
        if (!isServer) return;

        float splashDamageValue = absorbedAmount * PsiDissipationPercent;
        if (splashDamageValue <= 0f) return;

        Collider[] hits = Physics.OverlapSphere(_heroCharacter.transform.position, PsiDissipationRadius);

        foreach (var hit in hits)
        {
            Character target = hit.GetComponent<Character>();
            if (target == null) continue;
            if (target == _heroCharacter) continue;
            if (target.IsDead) continue;

            Damage splashDamage = new Damage
            {
                Value = splashDamageValue,
                Type = DamageType.Magical,
                School = Schools.Air,
                Form = AbilityForm.Magic,
            };

            target.Health.TryTakeDamage(ref splashDamage, skill);
        }
    }

    public void ConvertToAttackingEnergy(float amount)
    {
        float transferAmount = Mathf.Min(CurrentValue, amount);
        if (transferAmount > 0)
        {
            UsePsiEnergy(transferAmount);
            _attackingPsionicEnergy.ReceiveAttackingEnergy(transferAmount);
        }
    }

    [ClientRpc]
    private void RpcInternalPsiEnergyChanged(bool value)
    {
        _isInternalPsiEnergy = value;
    }

    [ClientRpc]
    private void RpcOnEnergyChanged(float value)
    {
        CurrentValue = value;
        OnEnergyChanged?.Invoke(value);
    }

    public void ShowPhantomValue(Damage phantomValue)
    {
        throw new NotImplementedException();
    }

    public override void Add(float value)
    {
        if (psionicEnergySkill == null || !psionicEnergySkill.IsPsiEnergyActive) return;

        base.Add(value);
    }
    
    protected override IEnumerator RegenerateJob()
    {
        yield return null;
    }
}