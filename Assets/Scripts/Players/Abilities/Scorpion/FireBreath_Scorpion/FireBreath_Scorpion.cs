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
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _damageRate = 0.5f;
    [SerializeField] private float _damageScalePerTick = 2f;

    [Header("Range Settings")]
    [SerializeField] private float _maxDistance = 4f;
    [SerializeField] private float _minDistance = 1f;
    [SerializeField] private float _coneAngle = 45f;

    private FireBreath_Prefab _fireBreathInstance;
    private Dictionary<Health, int> _enemiesDict = new Dictionary<Health, int>();
    private WaitForSeconds _waitForApplyFireBreathDamage;

    public ConsumeCombo_Scorpion Notifier { get; set; }
    public int ConsumedAmount { get; set; }
    
    public event Action<GameObject, Skill> OnDamaged;

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

    private void Start()
    {
        _waitForApplyFireBreathDamage = new WaitForSeconds(ApplyFireBreathDamageTickInterval);
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        return;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (!GetMouseButton)
        {
            callbackDataSaved(new TargetInfo());
            yield return null;
        }
    }

    protected override IEnumerator CastJob()
    {
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

                float finalDamageValue = Buff.Damage.GetBuffedValue(_damage * damageScale * distanceMultiplier);

                Damage damage = new Damage
                {
                    Value = finalDamageValue,
                    Type = Info.DamageType
                };

                CmdApplyDamage(damage, enemy.gameObject);

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

            elapsed += ApplyFireBreathDamageTickInterval;

            yield return _waitForApplyFireBreathDamage;

            baseDamage *= 2;
        }
        Hero.Move.SetCanMove(true);
        CmdDestroyFireBreath();
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
            float duration = ScorchedSoulDuration;
            stateManager.AddState(States.ScorchedSoul, duration, 0, _hero.gameObject, Name);
        }
    }

    [Command]
    private void CmdRotateFireBreath(Quaternion rotation)
    {
        if (_fireBreathInstance != null)
            _fireBreathInstance.transform.rotation = rotation;
    }

    private void ApplyDamageToEnemiesInCone()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, _maxDistance, _targetsLayers);

        foreach (Collider collider in hitColliders)
        {
            if ((_targetsLayers.value & (1 << collider.gameObject.layer)) == 0)
                continue;

            if (collider.TryGetComponent<Health>(out Health enemy))
            {
                Vector3 dirToEnemy = (enemy.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, dirToEnemy);

                if (angle <= _coneAngle / 2 && !Physics.Linecast(transform.position, enemy.transform.position, _targetsLayers))
                {
                    float distanceMultiplier = CalculateDistanceMultiplier(enemy.transform.position);
                    int damageScale = _enemiesDict.ContainsKey(enemy) ? _enemiesDict[enemy] : 1;

                    float finalDamageValue = Buff.Damage.GetBuffedValue(_damage * distanceMultiplier * damageScale);

                    Damage damage = new Damage
                    {
                        Value = finalDamageValue,
                        Type = Info.DamageType,
                    };

                    CmdApplyDamage(damage, enemy.gameObject);

                    if (_enemiesDict.ContainsKey(enemy))
                        _enemiesDict[enemy] *= (int)_damageScalePerTick;
                    else
                        _enemiesDict[enemy] = (int)_damageScalePerTick;
                }
            }
        }
    }

    private float CalculateDistanceMultiplier(Vector3 enemyPos)
    {
        float distance = Vector3.Distance(transform.position, enemyPos);
        distance = Mathf.Clamp(distance, _minDistance, _maxDistance);

        float normalized = (distance - _minDistance) / (_maxDistance - _minDistance);
        return Mathf.Lerp(BaseDamageScale, MinDistanceMultiplier, normalized);
    }

    protected override void ClearData()
    {
        _enemiesDict.Clear();

        if (_fireBreathInstance != null) Destroy(_fireBreathInstance.gameObject);
    }

    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, MaxDistanceRayCast, ~0))
            return hit.point;

        return transform.position + transform.forward * FallbackMouseForwardDistance;
    }

    //public void TryUpgradeByConsumingCombo(int amount)
    //{
    //    if (!Notifier.IsActive)
    //    {
    //        ConsumedAmount = 0;
    //        return;
    //    }
    //    ConsumedAmount = Notifier.PayComboPoints(Mathf.Clamp(amount, 0, Notifier.AvailablePoints));
    //}
}
