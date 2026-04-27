using Mirror;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BasePsionicEnergy : Resource, IDamageable
{
    [SerializeField] private Character _player;
    [SerializeField] private AttackingPsionicEnergy _attackingPsionicEnergy;
    [SerializeField] private Slider basePsionicsSlider;
    [SerializeField] private PsionicEnergySkill psionicEnergySkill;

    private const float BasePsionicaThreshold = 30f;
    private const float BaseSliderFillPercent = 0.3f;
    private const float RemainingSliderFillPercent = 0.7f;
    private const float DamageToPsiConversionRate = 0.1f;
    private const float DistanceStep = 1f;
    private const float PsiPercentPerStep = 0.01f;

    private float _psionicaDecayTime;
    private Vector3 _lastPosition;
    private float _distanceAccumulator;
    private bool _isInternalPsiEnergy = false;
    private bool _isAccumulationPsionicRunning = false;
    private bool _isTakesAnyDamage = false;
    private Coroutine _energyDecayCoroutine;

    private float MaxPsi => _player.Health.MaxValue;
    public bool IsAttackingPsiEnergyActive => _attackingPsionicEnergy.IsAttackingPsiEnergy;
    
    public event Action<Damage, Skill> DamageTaken;
    public event Action<float> OnEnergyChanged;
    public event Action<bool> OnAccumulationPsionicChanged;

    public PsionicEnergySkill PsionicEnergySkill { get => psionicEnergySkill; set => psionicEnergySkill = value; }
    public float PsionicaDecayTime { get => _psionicaDecayTime; set => _psionicaDecayTime = value; }

    private void Start()
    {
        _psionicaDecayTime = psionicEnergySkill.Cooldown.CooldownTime;
    }

    public override void Initialize(Attribute maxValue, Attribute regenValue, CharacterData data)
    {
        base.Initialize(maxValue, regenValue, data);
    }

    public void TakesAnyDamage(bool value) => _isTakesAnyDamage = value;
    public void AccumulationPsionicChanged(bool value) => OnAccumulationPsionicChanged?.Invoke(value);
    public void AccumulationPsionicRunning(bool value)
    {
        _isAccumulationPsionicRunning = value;
        _lastPosition = _player.transform.position;
        _distanceAccumulator = 0f;
    }

    public override void Init(ResourceAttribute resource)
    {
        base.Init(resource);

        if (_player != null)
        {
            _maxValue = _player.AttributeSystem.HPMax.GetValue();
            _player.Health.Shields.Add(this);
        }
    }

    private void Update()
    {
        UpdatePsionicaBar();
        PsionicRunning();
    }

    private void OnEnable()
    {
        if (_player.DamageTracker != null)
        {
            _player.DamageTracker.OnDamageTracked += OnDamageDealt;
        }
        if (_player.Health != null) _player.Health.OnBeforeDamage += psionicEnergySkill.HandleIncomingDamage;

        if (_player.SpawnComponent != null)
        {
            _player.SpawnComponent.UnitAdded += OnMinionSpawned;
            _player.SpawnComponent.UnitRemoved += OnMinionRemoved;
        }

        _player.Reset += PsiEnergyDecayServer;
    }

    private void OnDisable()
    {
        if (_player != null && _player.DamageTracker != null) _player.DamageTracker.OnDamageTracked -= OnDamageDealt;
        if (_player.Health != null) _player.Health.OnBeforeDamage -= psionicEnergySkill.HandleIncomingDamage;

        if (_player.SpawnComponent != null)
        {
            _player.SpawnComponent.UnitAdded -= OnMinionSpawned;
            _player.SpawnComponent.UnitRemoved -= OnMinionRemoved;

            foreach (var unit in _player.SpawnComponent.Units)
            {
                if (unit != null && unit.DamageTracker != null) unit.DamageTracker.OnDamageTracked -= OnDamageDealt;
            }
        }

        _player.Reset -= PsiEnergyDecayServer;
    }

    private void PsionicRunning()
    {
        if (!_isAccumulationPsionicRunning) return;
        if (!_attackingPsionicEnergy.IsAttackingPsiEnergy) return;

        Vector3 currentPos = _player.transform.position;
        float distanceDelta = Vector3.Distance(currentPos, _lastPosition);
        if (distanceDelta <= 0.001f) return;

        _distanceAccumulator += distanceDelta;

        if (_distanceAccumulator >= DistanceStep)
        {
            int steps = Mathf.FloorToInt(_distanceAccumulator / DistanceStep);
            _distanceAccumulator -= steps * DistanceStep;
            float psiGain = MaxPsi * PsiPercentPerStep * steps;
            AddPsiAndRestartDecay(psiGain);
        }

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

            return true;
        }

        return false;
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
}
