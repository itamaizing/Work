using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FireBreath_Scorpion : Skill, ICanConsumeComboPoints
{
    [Header("Ability Settings")]
    [SerializeField] private FireBreath_Prefab _conePrefab;
    [SerializeField] private GameObject _prefab;
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private string enemyTag;

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

    public ConsumeCombo_Scorpion Notifier { get; set; }
    public int ConsumedAmount { get; set; }

    protected override bool IsCanCast => true;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    protected override IEnumerator PrepareJob()
    {
        while (!GetMouseButton)
            yield return null;
    }

    protected override IEnumerator CastJob()
    {
        CmdSpawnFireBreath();
        yield return StartCoroutine(ApplyFireBreathDamage());
    }

    private IEnumerator ApplyFireBreathDamage()
    {
        float elapsed = 0f;
        Hero.Move.CanMove = false;

        while (elapsed < CastStreamDuration)
        {
            ApplyDamageToEnemiesInCone();
            elapsed += _damageRate;
            yield return new WaitForSeconds(_damageRate);
        }

        Hero.Move.CanMove = true;
        CmdDestroyFireBreath();
    }

    [Command]
    private void CmdSpawnFireBreath()
    {
        Vector3 spawnPosition = transform.position + Vector3.up * 1.5f;

        var fireObj = Instantiate(_prefab, spawnPosition, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(fireObj, _hero.NetworkSettings.MyRoom);
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

    private IEnumerator FollowMouseRoutine()
    {
        while (_fireBreathInstance != null)
        {
            Vector3 mousePos = GetMouseWorldPosition();
            Vector3 direction = (mousePos - transform.position);
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                CmdRotateFireBreath(targetRotation);
            }

            yield return null;
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
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, _maxDistance, enemyLayerMask);

        foreach (Collider collider in hitColliders)
        {
            if (!collider.CompareTag(enemyTag))
                continue;

            if (collider.TryGetComponent<Health>(out Health enemy))
            {
                Vector3 dirToEnemy = (enemy.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, dirToEnemy);

                if (angle <= _coneAngle / 2 && !Physics.Linecast(transform.position, enemy.transform.position, enemyLayerMask))
                {
                    float distanceMultiplier = CalculateDistanceMultiplier(enemy.transform.position);
                    int damageScale = _enemiesDict.ContainsKey(enemy) ? _enemiesDict[enemy] : 1;

                    float finalDamageValue = Buff.Damage.GetBuffedValue(_damage * distanceMultiplier * damageScale);

                    Damage damage = new Damage
                    {
                        Value = finalDamageValue,
                        Type = DamageType,
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
        return Mathf.Lerp(1f, 0.7f, normalized);
    }

    protected override void ClearData()
    {
        _enemiesDict.Clear();
        if (_fireBreathInstance != null)
            Destroy(_fireBreathInstance.gameObject);
    }

    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, ~0))
            return hit.point;

        return transform.position + transform.forward * 5f;
    }

    public void TryUpgradeByConsumingCombo(int amount)
    {
        if (!Notifier.IsActive)
        {
            ConsumedAmount = 0;
            return;
        }
        ConsumedAmount = Notifier.PayComboPoints(Mathf.Clamp(amount, 0, Notifier.AvailablePoints));
    }
}
