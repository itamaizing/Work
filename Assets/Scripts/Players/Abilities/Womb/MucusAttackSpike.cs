using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MucusAttackSpike : NetworkBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator _attackSpike;
    [SerializeField] private Transform _damagePoint;
    [SerializeField] private Skill _skill;

    [Header("Settings")]
    [SerializeField] private float tickInterval = 2f;
    [SerializeField] private float damageRadius = 1f;
    [SerializeField] private float damageValue = 10f;
    [SerializeField] private LayerMask characterLayer;
    [SerializeField] private DamageType damageType = DamageType.Physical;
    [SerializeField] private Schools school = Schools.Air;

    private readonly List<Character> _charactersInTrigger = new();

    private Coroutine _attackCoroutine;
    private WaitForSeconds _wait;

    private static readonly int AttackSpikeHash = Animator.StringToHash("AttackSpike");

    private void Awake()
    {
        _wait = new WaitForSeconds(tickInterval);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out Character character)) return;
        if (_charactersInTrigger.Contains(character)) return;

        _charactersInTrigger.Add(character);

        if (_attackCoroutine == null)
            _attackCoroutine = StartCoroutine(AttackRoutine());
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.TryGetComponent(out Character character)) return;

        _charactersInTrigger.Remove(character);

        if (_charactersInTrigger.Count == 0)
            StopAttack();
    }

    private void StopAttack()
    {
        if (_attackCoroutine != null)
        {
            StopCoroutine(_attackCoroutine);
            _attackCoroutine = null;
        }
    }

    private IEnumerator AttackRoutine()
    {
        Debug.Log("Spike attack started");

        while (_charactersInTrigger.Count > 0)
        {
            TriggerSpikeAttack();
            yield return _wait;
        }

        _attackCoroutine = null;
    }

    private void TriggerSpikeAttack()
    {
        if (_attackSpike != null)
            _attackSpike.SetTrigger(AttackSpikeHash);

        ApplyDamage();
    }

    private void ApplyDamage()
    {
        Collider[] hits = Physics.OverlapSphere(_damagePoint.position, damageRadius, characterLayer);

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent(out Character character)) continue;

            Damage damage = new Damage
            {
                Value = damageValue,
                Type = damageType,
                School = school
            };

            if (_skill != null)
                _skill.ApplyDamage(damage, character.gameObject);
        }
    }
}