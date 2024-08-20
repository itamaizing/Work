using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SoulDrainTest : TargetOrAreaAbility 
{
    [SerializeField] private Character _player;
    [SerializeField] private ParticleSystem _soulDrainPrefab;
    [SerializeField] private float _duration;
    private ParticleSystem _soulDrain;

    private float _damageDeal = 1.0f;
    private float _acceptHeal = 1.0f;

    private Character _currentTarget;

    private Coroutine _useCoroutine;

    public Vector3 playerPos;
    public Vector3 targetPos;
    Vector3 direction;
    float distance;


    private void Update()
    {
        playerPos = _player.transform.position;
        if (_currentTarget != null)
        {
            targetPos = _currentTarget.transform.position;

            float distancetoTarget = Vector3.Distance(playerPos, targetPos);
            distance = Mathf.Min(distancetoTarget, 10f); 

            direction = (playerPos - targetPos).normalized * (distance - 1.2f);
        }
    }

    protected override void CastAction()
    {
        _useCoroutine = StartCoroutine(UseAbilityCoroutine());
    }
    protected override void Cancel()
    {
        if (_useCoroutine != null)
        {
            StopCoroutine(UseAbilityCoroutine());
            _useCoroutine = null;
            _soulDrain.Clear();
        }
    }

    private IEnumerator UseAbilityCoroutine()
    {
        if (Target != null)
        {
            _currentTarget = Target;
            InstanceParticleSystem();
        }
        yield return null;
        Cancel();
    }

    private void DamageDeal()
    {
        CmdApplyDamage(_currentTarget.gameObject, _damageDeal, DamageType.Magical, AttackRangeType.RangeAttack);
    }

    private void AcceptHeal()
    {
        _player.Health.AddHeal(_acceptHeal);
    }

    private void InstanceParticleSystem()
    {
        if (_soulDrain == null && _currentTarget != null)
        {
            // Создаем систему частиц в позиции врага
            _soulDrain = Instantiate(_soulDrainPrefab, _currentTarget.transform.position, Quaternion.identity);

            float distancetoTarget = Vector3.Distance(_player.transform.position, _currentTarget.transform.position);
            float distance = Mathf.Min(distancetoTarget, 10f);

            Vector3 direction = (_player.transform.position - _currentTarget.transform.position).normalized * (distance - 1.2f);
            ParticleSystem.MainModule main = _soulDrain.main;
            main.startSpeed = 0;

            var velocityOverLifetime = _soulDrain.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;

            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(direction.x);
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(direction.y);
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(direction.z);

            _soulDrain.Play();
        }
        else
        {
            // Останавливаем и очищаем предыдущую систему частиц
            _soulDrain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = _soulDrain.main;
            main.duration = _duration;
            // Обновляем позицию и направление
            _soulDrain.transform.position = targetPos;
            var velocityOverLifetime = _soulDrain.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;

            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(direction.x);
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(direction.y);
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(direction.z);

            // Перезапуск системы частиц
            _soulDrain.Play();
        }
    }
}
