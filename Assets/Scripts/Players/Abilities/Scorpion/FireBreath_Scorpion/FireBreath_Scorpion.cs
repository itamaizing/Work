using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FireBreath_Scorpion : Skill, IComboParticipatingSkill
{
    [Header("Ability Settings")]
    [SerializeField] private FireBreath_Prefab _conePrefab;
    [SerializeField] private GameObject _prefab;
    [SerializeField] private float _duration = 3;

    [Header("Damage Settings")]
    [SerializeField] private float _damageScalePerTick = 2f;

    [Header("Range Settings")]
    [SerializeField] private float _maxDistance = 4f;
    [SerializeField] private float _minDistance = 1f;
    [SerializeField] private float _coneAngle = 45f;

    private FireBreath_Prefab _fireBreathInstance;
    private Dictionary<Health, int> _enemiesDict = new Dictionary<Health, int>();
    private WaitForSeconds _waitForApplyFireBreathDamage;

    private readonly Dictionary<Health, int> _exposureTicks = new();
    private readonly Dictionary<GameObject, int> _serverExposureTicks = new();

    public ConsumeCombo_Scorpion Notifier { get; set; }
    public int ConsumedAmount { get; set; }

    private bool _isIncreasedDamageExposure = false;

    public event IComboParticipatingSkill.OnBeforeApplyDamageDelegate OnBeforeApplyParticipatingDamage;
    public event Action<GameObject, Skill> OnDamaged;
    public event Action OnFireBreathStarted;

    protected override bool IsCanCast => true;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    #region Const
    private const float DebuffTickInterval = 0.3f;
    private const float ApplyFireBreathDamageTickInterval = 0.3f;
    private const float BaseScorchedSoulChance = 5f;
    private const float MaxScorchedSoulChance = 100f;
    private const float ScorchedSoulDuration = 3f;
    private const float MinRotationThresholdSqr = 0.01f;
    private const float FallbackMouseForwardDistance = 5f;
    private const float DamageLerpStart = 1f;
    private const float DamageLerpEnd = 0.7f;
    private const float BaseDamageScale = 1f;
    private const float MinDistanceMultiplier = 0.5f;
    private const float MaxDistanceRayCast = 100f;
    #endregion
    
    private float _lastEnergyTickPercent = 0f;
    private float _energyCostPerPercent = 1f; 
    
    #region Combo Speed Bonus
    [SerializeField] private float _speedBonusPerComboPoint = 0.20f;
    [SerializeField] private float _speedBonusPerFullCombo = 0.20f;
    private WaitForSeconds _currentWait;
    private float _originalCastDuration;
    private float _currentDurationReduction = 0f;
    private float _initialSpeed = 1f;
    private float _effectiveInterval;
    #endregion

    private void Start()
    {
        _originalCastDuration = _channelComponent.CastDuration;
        UpdateWaitInterval();
    }
    
    private void UpdateWaitInterval()
    {
        _effectiveInterval = ApplyFireBreathDamageTickInterval * _initialSpeed;
        _currentWait = new WaitForSeconds(_effectiveInterval);
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        return;
    }
    
    public float GetExposureMultiplier(Health target)
    {
        if (target == null || !_exposureTicks.TryGetValue(target, out int ticks)) return 1f;
        return 1f + ticks * 0.20f;
    }

    public void ClearExposureTicks()
    {
        _exposureTicks.Clear();
    }

    [Command]
    private void CmdClearServerExposureTicks()
    {
        _serverExposureTicks.Clear();
    }

    public void SetIncreasedExposuredDamage(bool value)
    {
        if(_isIncreasedDamageExposure == value) return;
        _isIncreasedDamageExposure = value;
        if(isClient)
            CmdSetIncreasedExposuredDamage(value);
    }

    [Command]
    private void CmdSetIncreasedExposuredDamage(bool value)
    {
        _isIncreasedDamageExposure = value;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (!GetMouseButton)
        {
            callbackDataSaved(new TargetInfo());
            yield return null;
        }
        OnFireBreathStarted?.Invoke();
    }

    protected override IEnumerator CastJob()
    {
        ClearExposureTicks();
        if(isClient)
            CmdClearServerExposureTicks();

        CmdSpawnFireBreath();
        yield return StartCoroutine(ApplyFireBreathDamage());
    }

    [Command]
    private void CmdSpawnFireBreath()
    {
        Vector3 spawnPosition = transform.position;

        var fireObj = Instantiate(_prefab, spawnPosition, Quaternion.identity);
        _fireBreathInstance = fireObj.GetComponent<FireBreath_Prefab>();
        fireObj.transform.SetParent(transform);

        fireObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        NetworkServer.Spawn(fireObj, connectionToClient);
        RpcInitializeFireBreath(fireObj);
    }

    [ClientRpc]
    private void RpcInitializeFireBreath(GameObject fireObj)
    {
        _fireBreathInstance = fireObj.GetComponent<FireBreath_Prefab>();
        StartCoroutine(FollowMouseRoutine());
    }

    [Command]
    private void CmdDestroyFireBreath()
    {
        if (_fireBreathInstance != null)
            NetworkServer.Destroy(_fireBreathInstance.gameObject);
    }

    private void TryApplyScorchedSoulDebuff(Health enemy, float elapsedTime)
    {
        CmdOnDamageEnd(enemy.gameObject);
        float baseChance = BaseScorchedSoulChance;
        int tickIndex = Mathf.FloorToInt(elapsedTime / DebuffTickInterval);
        float currentChance = baseChance * Mathf.Pow(2, tickIndex);

        currentChance = Mathf.Clamp(currentChance, 0f, MaxScorchedSoulChance);

        float roll = UnityEngine.Random.Range(0f, MaxScorchedSoulChance);
        if (roll <= currentChance)
        {
            if (enemy.TryGetComponent<CharacterState>(out var stateManager))
            {
                CmdApplyScorchedSoulDebuff(stateManager.netIdentity);
            }
        }
    }

    [Command]
    private void CmdOnDamageEnd(GameObject target)
    {
        OnDamaged?.Invoke(target, this);
        _serverExposureTicks[target] = _serverExposureTicks.GetValueOrDefault(target, 0) + 1;

        var ignition = target.GetComponent<CharacterState>()?.GetState(States.Ignition) as IgnitionState;
        if (ignition != null && _isIncreasedDamageExposure)
            ignition.UpdateFireBreathBonus(_serverExposureTicks[target] * 0.20f);
    }

    private void ApplyDamageAndDebuff(float elapsedTime, int dummyTickIndex)
    {
        Collider[] hitColliders = Physics.OverlapCapsule(
            transform.position,
            transform.position + transform.forward * _maxDistance,
            _coneAngle,
            _targetsLayers);

        foreach (Collider collider in hitColliders)
        {
            if ((_targetsLayers.value & (1 << collider.gameObject.layer)) == 0)
                continue;

            if (collider.TryGetComponent<Health>(out Health enemy))
            {
                Vector3 dirToEnemy = (enemy.transform.position - transform.position).normalized;
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance > _maxDistance)
                    continue;

                float angle = Vector3.Angle(transform.forward, dirToEnemy);
                if (angle > _coneAngle / 2f)
                    continue;

                float distanceMultiplier = Mathf.Lerp(DamageLerpStart, DamageLerpEnd, (distance / _maxDistance));
                int damageScale = _enemiesDict.ContainsKey(enemy) ? _enemiesDict[enemy] : 1;

                float finalDamageValue = Buff.Damage.GetBuffedValue(Damage * damageScale * distanceMultiplier);

                Damage damage = new Damage
                {
                    Value = finalDamageValue,
                    Type = Info.DamageType,
                    School = Schools.Fire
                };

                CmdApplyDamage(damage, enemy.gameObject);
                
                if(_isIncreasedDamageExposure)
                    _exposureTicks[enemy] = _exposureTicks.GetValueOrDefault(enemy, 0) + 1;

                if (_enemiesDict.ContainsKey(enemy))
                    _enemiesDict[enemy] *= (int)_damageScalePerTick;
                else
                    _enemiesDict[enemy] = 2;

                TryApplyScorchedSoulDebuff(enemy, elapsedTime);
            }
        }
    }

    private IEnumerator ApplyFireBreathDamage()
    {
        float elapsed = 0f;
        int baseDamage = 1;

        Hero.Move.SetCanMove(false);

        while (elapsed < CastStreamDuration)
        {
            ApplyDamageAndDebuff(elapsed, baseDamage);

            elapsed += _effectiveInterval;
                
            TrySpendEnergy(elapsed);

            yield return _currentWait;

            baseDamage *= 2;
        }
        Hero.Move.SetCanMove(true);
        CmdDestroyFireBreath();
    }
    
    protected override void CommitUse()
    {
        UseCooldownOrCharges();
    }

    private void TrySpendEnergy(float elapsedTime)
    {
        if (_hero == null) return;

        float progressPercent = (elapsedTime / CastStreamDuration) * 100f;
        float current10PercentBlocks = Mathf.Floor(progressPercent / 10f);

        int blocksToSpend = (int)(current10PercentBlocks - _lastEnergyTickPercent);

        if (blocksToSpend > 0)
        {
            float energyToSpend = blocksToSpend * _energyCostPerPercent;

            Cost.TryPayMandatory();
            
            if (_hero.Resources.TryGetValue(ResourceType.Energy, out var energyResource))
            {
                //energyResource.CmdUse(energyToSpend);
                energyResource.CmdAddRegenModifier(energyToSpend,2,isFast:false);
            }

            _lastEnergyTickPercent = current10PercentBlocks;
        }
    }

    private IEnumerator FollowMouseRoutine()
    {
        while (_fireBreathInstance != null)
        {
            Vector3 mousePos = GetMouseWorldPosition();
            Vector3 direction = (mousePos - transform.position);
            direction.y = 0f;

            if (direction.sqrMagnitude > MinRotationThresholdSqr)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                CmdRotateFireBreath(targetRotation);
            }

            yield return null;
        }
    }

    [Command]
    private void CmdApplyScorchedSoulDebuff(NetworkIdentity targetIdentity)
    {
        if (targetIdentity.TryGetComponent<CharacterState>(out var stateManager))
        {
            stateManager.AddState(States.ScorchedSoul, 6, 0, _hero.gameObject, Name);
        }
    }

    [Command]
    private void CmdRotateFireBreath(Quaternion rotation)
    {
        if (_fireBreathInstance != null)
            _fireBreathInstance.transform.rotation = rotation;
    }

    protected override void ClearData()
    {
        CmdClearData();
        _enemiesDict.Clear();
        _lastEnergyTickPercent = 0f;
        _currentDurationReduction = 0f;
        _initialSpeed = 1f;
        _channelComponent.CastDuration = _originalCastDuration;
        UpdateWaitInterval();
        
        if (_fireBreathInstance != null) 
            Destroy(_fireBreathInstance.gameObject);
    }

    [Command]
    private void CmdClearData()
    {
        _currentDurationReduction = 0f;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, MaxDistanceRayCast, ~0))
            return hit.point;

        return transform.position + transform.forward * FallbackMouseForwardDistance;
    }
    
    public void OnTargetHasComboPoint(GameObject target, float comboPoints)
    {
        if (_currentDurationReduction <= 0)
            _currentDurationReduction = comboPoints * _speedBonusPerComboPoint;
        else
            _currentDurationReduction += comboPoints * _speedBonusPerComboPoint;

        _channelComponent.CastDuration = _originalCastDuration * (1f - _currentDurationReduction);
        _channelComponent.CastDuration = Mathf.Max(_channelComponent.CastDuration, _originalCastDuration * 0.2f);
        
        RpcApplyCastDuration(_currentDurationReduction, _channelComponent.CastDuration);
    }

    public void OnFinalComboSkill(GameObject target)
    {
        if (_currentDurationReduction <= 0)
            _currentDurationReduction = _speedBonusPerFullCombo;
        else
            _currentDurationReduction += _speedBonusPerFullCombo;
        _channelComponent.CastDuration = _originalCastDuration * (1f - _currentDurationReduction);
        _channelComponent.CastDuration = Mathf.Max(_channelComponent.CastDuration, _originalCastDuration * 0.2f);


        RpcApplyCastDuration(_currentDurationReduction, _channelComponent.CastDuration);
    }
    
    [ClientRpc]
    private void RpcApplyCastDuration(float newSpeedMultiplier, float newCastDuration)
    {
        _initialSpeed = 1f;
        _initialSpeed -= newSpeedMultiplier;
        _channelComponent.CastDuration = newCastDuration;
        
        UpdateWaitInterval();
    }
}
