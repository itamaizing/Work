using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class PoisonCloudBuff : BaseEffect
{
    [Header("Buff Components")]
    [SerializeField] private CircleCollider2D _triggerCircleCollider;
    [SerializeField] private ParticleSystem _poisonCloudPrefab;
    [SerializeField] private ParticleSystem _instancePoisonCloud;
    private Character _caster;

    [Header("Buff Values")]
    [SerializeField] private int _maxStacks = 5;
    [SerializeField] private float _duration = 6;
    [SerializeField] private float _radiusCloud;
    private int _currentStacks = 0;
    private float _currentDamage;
    private float _increasedDamage;
    private float _baseDamage = 0.005f;
    private float _timeBetweenAttack = 1.0f;

    [Header("Coroutines")]
    private Coroutine _useCoroutine;
    private Coroutine _lifeTimeStacksCoroutine;
    private Coroutine _damageDealCoroutine;

    private void Start()
    {
        _radiusCloud = (2f * GlobalVariable.cellSize) / GlobalVariable.cellSize;
        _triggerCircleCollider.radius = _radiusCloud;
    }

    public void PoisonCloudAddStacks(Character caster)
    {
        _caster = caster;

        if (_currentStacks < _maxStacks)
        {
            _currentStacks++;
            _increasedDamage = _currentStacks * _baseDamage;

            if (_useCoroutine == null)
            {
                _useCoroutine = StartCoroutine(ActivatePoisonCloud());
            }
            else
            {
                UpdateInstancePoisonCloud();

                if (_lifeTimeStacksCoroutine != null)
                    StopCoroutine(_lifeTimeStacksCoroutine);
            }

            _lifeTimeStacksCoroutine = StartCoroutine(LifeTimeStacks());

        }
        else if (_currentStacks == _maxStacks)
        {
            ResetLifeTimeStacks();
        }
    }

    private void ResetLifeTimeStacks()
    {
        //Метод для обновления таймера стаков
        if (_lifeTimeStacksCoroutine != null)
        {
            StopCoroutine(_lifeTimeStacksCoroutine);
        }

        _lifeTimeStacksCoroutine = StartCoroutine(LifeTimeStacks());
    }

    #region OnTrigger

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.transform != _caster.transform)
        {
            if (collision.TryGetComponent<HealthComponent>(out var target))
            {
                if (_damageDealCoroutine == null)
                {
                    _damageDealCoroutine = StartCoroutine(DealDamage());
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (_damageDealCoroutine != null && collision.TryGetComponent<HealthComponent>(out var target))
        {
            StopCoroutine(_damageDealCoroutine);
            _damageDealCoroutine = null;
        }
    }

    #endregion

    #region InstancePoisonCloud

    private void InstantiateCloud()
    {
        if (_instancePoisonCloud == null)
        {
            _instancePoisonCloud = Instantiate(_poisonCloudPrefab, transform.position, Quaternion.identity);
        }

        _instancePoisonCloud.Play();
    }

    private void UpdateInstancePoisonCloud()
    {
        if (_instancePoisonCloud != null)
        {
            _instancePoisonCloud.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = _instancePoisonCloud.main;
            main.duration = _duration;
            _instancePoisonCloud.Play();
        }
    }
    private void Update()
    {
        // Нужно для того, чтобы PoisonCloud оставался на игроке при передвижении игрока.
        if (_instancePoisonCloud != null)
        {
            _instancePoisonCloud.transform.position = _caster.transform.position;
        }
    }

    #endregion

    #region Coroutines

    private IEnumerator ActivatePoisonCloud()
    {
        InstantiateCloud();
        yield return null;
    }

    private IEnumerator DealDamage()
    {
        while (_currentStacks > 0)
        {
            Collider2D[] hitTargets = Physics2D.OverlapCircleAll(_caster.transform.position, _radiusCloud);
            foreach (var targets in hitTargets)
            {
                if (targets.TryGetComponent<HealthComponent>(out var target) && target.gameObject != _caster.gameObject)
                {
                    _currentDamage = target.MaxHealth * _increasedDamage;

                    target.TryTakeDamage(_currentDamage, DamageType.Physical, AttackRangeType.MeleeAttack);
                }
            }
            yield return new WaitForSeconds(_timeBetweenAttack);
        }
    }

    private IEnumerator LifeTimeStacks()
    {
        yield return new WaitForSeconds(_duration);

        while (_currentStacks > 0)
        {
            _currentStacks--;
        }

        if (_currentStacks == 0)
        {
            if (_instancePoisonCloud != null)
            {
                _instancePoisonCloud.transform.parent = null;
                _instancePoisonCloud.Stop();
                Destroy(_instancePoisonCloud.gameObject);
                _instancePoisonCloud = null;
            }

            StopAllCoroutines();
            Destroy(gameObject);
        }
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _radiusCloud);
    }
}
