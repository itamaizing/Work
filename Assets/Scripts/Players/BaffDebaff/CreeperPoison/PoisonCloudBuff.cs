using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

public class PoisonCloudBuff : BaseEffect
{
    [Header("Talents")]
    [SerializeField] private HealingPoisonCloud _healingPoisonCloud;
    [SerializeField] private CapaciousPoisonCloud _capaciousPoisonCloud;

    [Header("Buff Components")]
    [SerializeField] private Character _dad;
    [SerializeField] private CircleCollider2D _triggerCircleCollider;
    [SerializeField] private ParticleSystem _poisonCloudPrefab;
    [SerializeField] private ParticleSystem _instancePoisonCloud;

    [Header("Buff Values")]
    [SerializeField] private int _maxStacks = 5;
    [SerializeField] private float _duration = 6;

    private int _currentStacks = 0;
    private float _currentDamage;
    private float _increasedDamage;
    private float _baseDamage = 0.005f;
    private float _timeBetweenAttack = 1.0f;
    private float _radiusCloud = 0.5f;

    private Coroutine _useCoroutine;
    private Coroutine _lifeTimeStacksCoroutine;
    private Coroutine _damageDealCoroutine;
    private Coroutine _healPoisonCloudTalentCoroutine;

    #region ForTalents

    private float _maxHealth;
    private float _baseHealthRegen;
    private float _currentHealthRegen;
    private float _increaseHealthRegen = 0.005f;
    private float _newRadiusCloud = 1.5f;

    private bool _isActiveCapaciousCloud;

    #endregion

    public float RadiusCloud { get => _radiusCloud; set => _radiusCloud = value; }
    public int CurrentStacks { get => _currentStacks; set => _currentStacks = value; }

    public void PoisonCloudAddStacks(Character caster, bool isActiveHealingCloud, bool isActiveCapaciousCloud)
    {
        Debug.Log("PoisonCloudAddStacks work");
        _dad = caster;

        _isActiveCapaciousCloud = isActiveCapaciousCloud;

        _healingPoisonCloud = _dad.GetComponentInChildren<HealingPoisonCloud>();
        _capaciousPoisonCloud = _dad.GetComponentInChildren<CapaciousPoisonCloud>();
        
        if (_currentStacks < _maxStacks)
        {
            Debug.Log("PoisonCloudAddStacks if CurrentStacks == " + _currentStacks);
            _currentStacks++;
            _increasedDamage = _currentStacks * _baseDamage;

            if (isActiveHealingCloud)
            {
                _currentHealthRegen = _currentStacks * _increaseHealthRegen;

                if (_healPoisonCloudTalentCoroutine == null)
                {
                    Debug.Log("Work healCLoud");

                    _maxHealth = _dad.Health.MaxHealth;
                    _baseHealthRegen = _dad.Health.HpRegenerationValue;
                    _healPoisonCloudTalentCoroutine = StartCoroutine(HealingCloud());
                }
            }

            if (_useCoroutine == null)
            {
                Debug.Log("if use coroutine == null");
                _useCoroutine = StartCoroutine(ActivatePoisonCloud());
            }
            else
            {
                Debug.Log("else use coroutine == null");
                UpdateInstancePoisonCloud();

                if (_lifeTimeStacksCoroutine != null)
                    StopCoroutine(_lifeTimeStacksCoroutine);
            }

            _lifeTimeStacksCoroutine = StartCoroutine(LifeTimeStacks());

        }
        else if (_currentStacks == _maxStacks)
        {
            Debug.Log("else if CurrentStacks == _maxStacks");
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
        if (collision.gameObject.transform != _dad.transform)
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
        if (_isActiveCapaciousCloud)
        {
            _radiusCloud = 4f + _newRadiusCloud;
        }
        else if (!_isActiveCapaciousCloud)
        {
            _radiusCloud = 4f;
        }
        _triggerCircleCollider.radius = _radiusCloud;

        if (_instancePoisonCloud != null)
        {
            _instancePoisonCloud.transform.position = _dad.transform.position;
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
            Collider2D[] hitTargets = Physics2D.OverlapCircleAll(_dad.transform.position, _radiusCloud);
            foreach (var targets in hitTargets)
            {
                if (targets.TryGetComponent<HealthComponent>(out var target) && target.gameObject != _dad.gameObject)
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

            _dad.Health.HpRegenerationValue = _baseHealthRegen;
            StopAllCoroutines();
            Destroy(gameObject);
        }
    }

    private IEnumerator HealingCloud()
    {
        float originalHpRegen = _dad.Health.HpRegenerationValue;

        while (_currentStacks != 0)
        {
            float increasedHealthRegen = _maxHealth * _currentHealthRegen + originalHpRegen;

            _dad.Health.HpRegenerationValue = increasedHealthRegen;

            yield return null;
        }

        _dad.Health.HpRegenerationValue = originalHpRegen;
    }

    #endregion
}
