using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PoisonCloudBuff : BaseEffect
{
    [Header("Buff Components")]
    [SerializeField] private List<GameObject> _targets = new();
    [SerializeField] private CircleCollider2D _radiusSpeel;
    [SerializeField] private ParticleSystem _poisonCloudPrefab;
    [SerializeField] private ParticleSystem _poisonCloudInstance;
    private Character _dad;
    private HealthComponent _target;

    [Header("Buff Values")]
    [SerializeField] private int _maxStacks = 5;
    [SerializeField] private float _duration = 6;
    [SerializeField] private float _baseDamage = 0.5f;
    [SerializeField] private float _radiusCloud = 0.5f * GlobalVariable.cellSize;
    private int _currentStacks = 1;
    private float _currentDamage;
    private float _timeBetweenAttack = 1.0f;

    private Coroutine _useCoroutine;
    private Coroutine _lifeTimeStacksCoroutine;
    private Coroutine _damageDealCoroutine;
    private Coroutine _destroyPrefabCoroutine;

    public void PoisonCloudAddStacks(Character dad)
    {
        _dad = dad;

        if (_currentStacks < _maxStacks)
        {
            _currentStacks++;
            Debug.Log("_CurrentStacks == " + _currentStacks);

            if (_useCoroutine == null)
            {
                _useCoroutine = StartCoroutine(UseCoroutine());
            }
            else
            {
                if (_poisonCloudInstance != null)
                {
                    _poisonCloudInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ParticleSystem.MainModule main = _poisonCloudInstance.main;
                    main.duration = _duration;
                    _poisonCloudInstance.Play();
                }

                if (_lifeTimeStacksCoroutine != null)
                {
                    StopCoroutine(_lifeTimeStacksCoroutine);
                }
            }

            _lifeTimeStacksCoroutine = StartCoroutine(LifeTimeStacks());

        }
        else if (_currentStacks == _maxStacks)
        {
            if (_lifeTimeStacksCoroutine != null)
            {
                StopCoroutine(_lifeTimeStacksCoroutine);
            }

            _lifeTimeStacksCoroutine = StartCoroutine(LifeTimeStacks());
        }
    }

    private void InstantiateParticle()
    {
        if (_poisonCloudInstance == null)
        {
            _poisonCloudInstance = Instantiate(_poisonCloudPrefab, transform.position, Quaternion.identity);
        }

        _poisonCloudInstance.Play();
    }

    private void Update()
    {
        if (_poisonCloudInstance != null)
        {
            _poisonCloudInstance.transform.position = _dad.transform.position;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.transform != _dad.transform)
        {
            if (collision.TryGetComponent<HealthComponent>(out var target))
            {
                _target = target;
                _damageDealCoroutine = StartCoroutine(DamageDeal(_target));
            }
        }
    }

    #region Coroutines

    private IEnumerator UseCoroutine()
    {
        _currentStacks = 1;
        _radiusSpeel.radius = _radiusCloud;
        InstantiateParticle();
        yield return null;
    }

    private IEnumerator DamageDeal(HealthComponent target)
    {
        while (_currentStacks > 0)
        {
            target.TryTakeDamage(_currentDamage, DamageType.Physical, AttackRangeType.MeleeAttack);

            yield return new WaitForSeconds(_timeBetweenAttack);
        }
    }


    private IEnumerator LifeTimeStacks()
    {
        yield return new WaitForSeconds(_duration);

        _currentStacks = 0;
        
        if (_currentStacks == 0)
        {
            if (_poisonCloudInstance != null)
            {
                _poisonCloudInstance.transform.parent = null;
                _poisonCloudInstance.Stop();
                Destroy(_poisonCloudInstance.gameObject);
                _poisonCloudInstance = null;
            }

            if (_useCoroutine != null)
                StopCoroutine(UseCoroutine());

            if (_damageDealCoroutine != null)
                StopCoroutine(DamageDeal(_target));

            Destroy(gameObject);
        }
    }

    #endregion
}
