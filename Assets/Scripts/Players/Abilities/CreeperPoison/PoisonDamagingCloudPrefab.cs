using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonDamagingCloudPrefab : NetworkBehaviour
{
    [SerializeField] private ParticleSystem _poisonDamagingCloudParticle;
    private ParticleSystem _instancePoisonDamagingCloud;

    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private float _damageTickRate = 1f;

    private Coroutine _damageCoroutine;

    [SerializeField] private int _maxStacks = 5;
    private int _currentStacks;

    [SerializeField] private float _radiusCloud;
    private float _baseDuration;
    private float _duration;

    [SerializeField] private float _damageModifier = 0.005f;

    private PoisonDamagingCloudPrefab _poisonDamageCloud;
    private Character _player;
    [ReadOnly][SerializeField] private Skill _skill;

    private Coroutine _lifetimeStacksCoroutine;
    private Coroutine _activateParticlePoisonCloudCoroutine;

    private Dictionary<Character, float> _poisonBoneTimers = new();
    private float _poisonBoneInterval = 3f;

    public PoisonDamagingCloudPrefab PoisonDamageCloud { get => _poisonDamageCloud; set => _poisonDamageCloud = value; }

    private void Update()
    {
        if (_instancePoisonDamagingCloud != null)
        {
            _instancePoisonDamagingCloud.transform.position = _player.transform.position;
        }
    }

    public void InitializationProjectile(Character player, float duration, Skill skill)
    {
        _player = player;
        _skill = skill;

        _duration = duration;
        _baseDuration = duration;
    }

    public void AddStack()
    {
        //Debug.Log("PoisonDamagingCloud / AddStack");
        //Debug.Log("PoisonDamagingCloud / AddStack / currentStacks = " + _currentStacks);
        if (_currentStacks < _maxStacks)
        {
            _currentStacks++;

            if (_activateParticlePoisonCloudCoroutine == null && _poisonDamageCloud == null)
            {
                _activateParticlePoisonCloudCoroutine = StartCoroutine(ActivatePoisonCloud());
            }
            else
            {
                UpdateInstanceCloud();
            }

            if (_lifetimeStacksCoroutine != null)
            {
                StopCoroutine(_lifetimeStacksCoroutine);
            }

            _duration = _baseDuration;
            _lifetimeStacksCoroutine = StartCoroutine(LifeTimeStacks());
        }
        else
        {
            UpdateInstanceCloud();

            if (_lifetimeStacksCoroutine != null)
            {
                StopCoroutine(_lifetimeStacksCoroutine);
            }

            _duration = _baseDuration;
            _lifetimeStacksCoroutine = StartCoroutine(LifeTimeStacks());
        }

        if (_damageCoroutine == null) _damageCoroutine = StartCoroutine(DamageEnemies());
    }

    private void InstantiateCloud()
    {
        if (_instancePoisonDamagingCloud == null)
        {
            _instancePoisonDamagingCloud = Instantiate(_poisonDamagingCloudParticle, _player.transform);
            _instancePoisonDamagingCloud.Play();
        }
    }

    private void UpdateInstanceCloud()
    {
        if (_instancePoisonDamagingCloud != null)
        {
            _instancePoisonDamagingCloud.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = _instancePoisonDamagingCloud.main;
            main.duration = _baseDuration;
            _instancePoisonDamagingCloud.Play();
        }
    }

    private IEnumerator DamageEnemies()
    {
        while (true)
        {
            DealDamageInRadius();
            yield return new WaitForSeconds(_damageTickRate);
        }
    }

    private IEnumerator ActivatePoisonCloud()
    {
        InstantiateCloud();
        yield return null;
    }

    private IEnumerator LifeTimeStacks()
    {
        float time = _duration;

        while (time > 0)
        {
            time -= Time.deltaTime;
            yield return null;
        }

        if (_activateParticlePoisonCloudCoroutine != null)
        {
            StopCoroutine(_activateParticlePoisonCloudCoroutine);
            _activateParticlePoisonCloudCoroutine = null;
        }

        if (_lifetimeStacksCoroutine != null)
        {
            StopCoroutine(_lifetimeStacksCoroutine);
            _lifetimeStacksCoroutine = null;
        }

        _currentStacks = 0;

        _instancePoisonDamagingCloud.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        Destroy(_instancePoisonDamagingCloud.gameObject);

        Destroy(gameObject);
        PoisonDamageCloud = null;
    }

    private void DealDamageInRadius()
    {
        if (_player == null) return;

        Collider[] targets = Physics.OverlapSphere( _player.transform.position, _radiusCloud, _enemyLayer);

        foreach (var col in targets)
        {
            if (col == null) continue;

            Character target = col.GetComponent<Character>();
            if (target == null) continue;
            if (target == _player) continue;
            if (target.IsDead) continue;

            float damageValue = target.Health.MaxValue * _damageModifier;

            Damage damage = new Damage
            {
                Value = damageValue,
                Type = DamageType.Magical,
                PhysicAttackType = AttackRangeType.RangeAttack
            };

            _skill.CmdApplyDamage(damage, target.gameObject);

            if (!_poisonBoneTimers.ContainsKey(target))
            {
                _poisonBoneTimers[target] = 0f;
            }

            _poisonBoneTimers[target] += _damageTickRate;

            if (_poisonBoneTimers[target] >= _poisonBoneInterval)
            {
                if (_skill.TryGetComponent<PoisonBall>(out PoisonBall poisonBall) && poisonBall.IsPoisonCloudAddPoisonBone)
                {
                    target.CharacterState.AddStateLogic( States.PoisonBone, 6, 0, Schools.None, _player.gameObject, null);
                }

                _poisonBoneTimers[target] = 0f;
            }
        }
    }

}
