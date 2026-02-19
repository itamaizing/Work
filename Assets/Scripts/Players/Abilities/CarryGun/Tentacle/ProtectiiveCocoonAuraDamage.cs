using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(NetworkIdentity))]
public class ProtectiiveCocoonAuraDamage : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float damageValue = 2f;
    [SerializeField] private float tickInterval = 1f;
    [SerializeField] private LayerMask characterLayer;

    private readonly List<Character> _charactersInZone = new();
    private WaitForSeconds _wait;
    private Coroutine _damageCoroutine;
    private Character _owner;

    private void Awake()
    {
        _wait = new WaitForSeconds(tickInterval);
    }

    public void Init(Character owner)
    {
        _owner = owner;
    }

    [Server]
    public void HandleTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out Character character))
            return;

        if (_charactersInZone.Contains(character))
            return;

        if (!IsEnemy(character))
            return;

        _charactersInZone.Add(character);

        if (_damageCoroutine == null)
            _damageCoroutine = StartCoroutine(DamageRoutine());
    }

    [Server]
    public void HandleTriggerExit(Collider other)
    {
        if (!other.TryGetComponent(out Character character))
            return;

        _charactersInZone.Remove(character);

        if (_charactersInZone.Count == 0 && _damageCoroutine != null)
        {
            StopCoroutine(_damageCoroutine);
            _damageCoroutine = null;
        }
    }

    [Server]
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

                ApplyDamageServer(character);
            }

            yield return _wait;
        }

        _damageCoroutine = null;
    }

    [Server]
    private void ApplyDamageServer(Character target)
    {
        if (!target.TryGetComponent(out IDamageable damageable))
            return;

        Damage damage = new Damage
        {
            Value = damageValue,
            Type = DamageType.Physical,
            School = Schools.Physical
        };

        damageable.TryTakeDamage(ref damage, null);

        if (_owner != null)
        {
            _owner.DamageTracker.AddDamage(
                damage,
                target.gameObject,
                isServerRequest: true
            );
        }
    }

    private bool IsEnemy(Character target)
    {
        if (_owner == null)
            return IsEnemyByLayer(target.gameObject);

        if (!_owner.TryGetComponent(out UserNetworkSettings ownerSettings) ||
            !target.TryGetComponent(out UserNetworkSettings targetSettings))
            return IsEnemyByLayer(target.gameObject);

        if (ownerSettings.TeamIndex == 0 || targetSettings.TeamIndex == 0)
            return IsEnemyByLayer(target.gameObject);

        return ownerSettings.TeamIndex != targetSettings.TeamIndex;
    }

    private bool IsEnemyByLayer(GameObject target)
    {
        return ((1 << target.layer) & characterLayer.value) != 0;
    }
}
