using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
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
    
    private float CurrentPsiDissipationRadius
    {
        get
        {
            var states = _heroCharacter != null ? _heroCharacter.CharacterState : null;

            bool generationActive = states != null && states.GetState(States.PsionicGeneration) != null;

            return PsiDissipationRadius * (generationActive ? 2f : 1f);
        }
    }
    
    public static float PsiPerMeter => 1f;
    
    public event Action<Damage, Skill> DamageTaken;
    public event Action<float> OnEnergyChanged;
    public event Action<bool> OnAccumulationPsionicChanged;

    public AttackingPsionicEnergy AttackingPsionicEnergy => _attackingPsionicEnergy;
    
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
    
    
    private bool _isPsionicsTalentActive = false;
    public bool IsPsionicsTalentActive => _isPsionicsTalentActive;

    public void AccumulationPsionicChanged(bool value)
    {
        if (_isPsionicsTalentActive == value) return;

        _isPsionicsTalentActive = value;
        OnAccumulationPsionicChanged?.Invoke(value);
    }

    public void DissipatingPsi(bool value)
    {
        if(value == _isDissipatingPsi) return;
        _isDissipatingPsi = value;
    }
    public void AccumulationPsionicRunning(bool value)
    {
        if(value == _isAccumulationPsionicRunning) return;
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
        if (_attackingPsionicEnergy == null || !_attackingPsionicEnergy.IsAttackingPsiEnergy) return;
        if (psionicEnergySkill == null || !psionicEnergySkill.IsPsiEnergyActive) return;

        Vector3 currentPos = _heroCharacter.transform.position;
        float distanceDelta = Vector3.Distance(currentPos, _lastPosition);
        if (distanceDelta <= 0.001f) return;
        
        float energyGain = distanceDelta * MaxPsi * 0.01f;
        if (energyGain > 0f)
            AddPsiAndRestartDecay(energyGain);

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
        return TryTakeDamage(ref damage, skill, _heroCharacter.transform.position);
    }

    [Server]
    public bool TryTakeDamage(ref Damage damage, Skill skill, Vector3 receiverPosition)
    {
        if (damage.Value <= 0f) return true;
        if (CurrentValue <= 0f) return false;

        float energySpent = Mathf.Min(CurrentValue, damage.Value);

        damage.Value = Mathf.Max(0f, damage.Value - energySpent * 0.5f);

        UsePsiEnergy(energySpent);

        _isInternalPsiEnergy = CurrentValue > 0f;
        RpcInternalPsiEnergyChanged(_isInternalPsiEnergy);
        UpdatePsionicaBar();

        DissipatePsiDamage(energySpent, receiverPosition);

        return true;
    }

    [Server]
    public void DissipatePsiDamage(float absorbedAmount, Vector3 center)
    {
        if (!_isDissipatingPsi) return;

        float damageValue = absorbedAmount * PsiDissipationPercent;
        if (damageValue <= 0f) return;

        var processedTargets = new HashSet<Character>();
        int damagedEnemies = 0;

        foreach (var hit in Physics.OverlapSphere(
                     center, CurrentPsiDissipationRadius))
        {
            var target = hit.GetComponentInParent<Character>();

            if (target == null || target == _heroCharacter || target.IsDead)
                continue;

            if (_heroCharacter.RelationTo(target) != CharacterRelation.Enemy)
                continue;

            if (!processedTargets.Add(target))
                continue;

            var splashDamage = new Damage
            {
                Value = damageValue,
                Type = DamageType.Magical,
                School = Schools.Air,
                Form = AbilityForm.Magic,
                SourceSkill = psionicEnergySkill
            };
            
            if (target.Health.TryTakeDamage(ref splashDamage, psionicEnergySkill) && splashDamage.Value > 0f)
                damagedEnemies++;
            
        }

        if (damagedEnemies == 0 || psionicEnergySkill == null || !psionicEnergySkill.IsExtendedDuration)
            return;

        float extension = damagedEnemies * 0.1f;

        _attackingPsionicEnergy?.ExtendDuration(extension);

        ImpatienceStateStacking.ExtendForCaster(_heroCharacter, extension);
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