using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProtectiiveCocoonAuraDamage : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float damageValue = 2f;
    [SerializeField] private float tickInterval = 1f;
    [SerializeField] private LayerMask characterLayer;
    [SerializeField] private DamageType damageType = DamageType.Physical;
    [SerializeField] private ProtectiveCocoon _protectiveCocoon;
    [SerializeField] private Transform _spawnPoint;

    [SerializeField] private LightningAttack _lightningPrefab;

    private readonly List<Character> _charactersInZone = new();
    private WaitForSeconds _wait;
    private Coroutine _damageCoroutine;

    private void Awake()
    {
        _wait = new WaitForSeconds(tickInterval);
    }

    private void OnDestroy()
    {
        if (_damageCoroutine != null)
        {
            StopCoroutine(_damageCoroutine);
            _damageCoroutine = null;
        }

        _charactersInZone.Clear();
    }

    [Server]
    public void HandleTriggerEnter(Collider other)
    {
        if (!_protectiveCocoon.IsProtectiveCooconSpawnAttack) return;
        if (!other.TryGetComponent(out Character character)) return;
        if (_charactersInZone.Contains(character)) return;
        if (!IsEnemy(character, other.gameObject)) return;

        _charactersInZone.Add(character);

        if (_damageCoroutine == null) _damageCoroutine = StartCoroutine(DamageRoutine());
    }

    [Server]
    public void HandleTriggerExit(Collider other)
    {
        if (!other.TryGetComponent(out Character character)) return;

        _charactersInZone.Remove(character);

        if (_charactersInZone.Count == 0 && _damageCoroutine != null)
        {
            StopCoroutine(_damageCoroutine);
            _damageCoroutine = null;
        }
    }

    private IEnumerator DamageRoutine()
    {
        while (_charactersInZone.Count > 0)
        {
            foreach (var character in _charactersInZone.ToArray())
            {
                if (character == null)
                {
                    _charactersInZone.Remove(character);
                    continue;
                }

                ApplyEnemy(character);
            }

            yield return _wait;
        }

        _damageCoroutine = null;
    }

    private void ApplyEnemy(Character character)
    {
        if (_protectiveCocoon.SkillHero == null) return;

        RpcSpawnLightning(character.netIdentity);
        ApplyDamage(damageValue, damageType, character.gameObject);
    }

    private void ApplyDamage(float value, DamageType type, GameObject target)
    {
        Damage damage = new Damage
        {
            Value = value,
            Type = type,
            School = _protectiveCocoon.SkillHero.School
        };

        _protectiveCocoon.SkillHero.ApplyDamage(damage, target);
    }

    private bool IsEnemy(Character characterTarget, GameObject target)
    {
        if (_protectiveCocoon.Hero == null) return IsEnemyByLayer(target);
        if (!_protectiveCocoon.Hero.TryGetComponent(out UserNetworkSettings ownerSettings) || !characterTarget.TryGetComponent(out UserNetworkSettings targetSettings)) return IsEnemyByLayer(target);
        if (!IsTeamAssigned(ownerSettings) || !IsTeamAssigned(targetSettings)) return IsEnemyByLayer(target);

        return ownerSettings.TeamIndex != targetSettings.TeamIndex;
    }

    private bool IsTeamAssigned(UserNetworkSettings settings)
    {
        return settings.TeamIndex != 0;
    }

    private bool IsEnemyByLayer(GameObject target)
    {
        return ((1 << target.layer) & characterLayer.value) != 0;
    }

    [ClientRpc]
    private void RpcSpawnLightning(NetworkIdentity targetIdentity)
    {
        if (targetIdentity == null) return;

        Vector3 spawnPoint = _spawnPoint.position;
        LightningAttack lightning = Instantiate(_lightningPrefab);

        lightning.Init(spawnPoint, targetIdentity.transform);
    }
}