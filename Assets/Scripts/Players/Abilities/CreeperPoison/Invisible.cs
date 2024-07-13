using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class Invisible : Ability
{
    [SerializeField] private Character _playerLinks;
    [SerializeField] private LayerMask _enemyLayerMask;
    [SerializeField] private LayerMask _obstacleLayerMask;
    [SerializeField] private CircleCollider2D _searchingCollider;

    [SerializeField] private float reduceMoveSpeed = 0.3f;
    [SerializeField] private float moveSpeedDecrease;

    private float _maxDistanceVisible = 6.0f;
    private float _timeWithoutDamage = 6.0f;
    private float _increaseEnergyRegen = 0.3f;

    public bool _enemyIsSees = false;
    public bool _isAttacked = false;
    public bool _isUsing = false;

    private HealthComponent _playerHealth;

    private Coroutine _useInvisibleCoroutine;
    private Coroutine _notAttackedCoroutine;
    private Coroutine _useJob;

    protected override void Start()
    {
        _playerHealth = _playerLinks.GetComponent<HealthComponent>();
    }

    protected override void Cast()
    {
        _useJob = StartCoroutine(UseCoroutine());
    }

    protected override void Cancel()
    {
        ResetAbility();
        _searchingCollider.radius = 0f;

        if (_useJob != null)
            StopCoroutine(UseCoroutine());

        if (_useInvisibleCoroutine != null)
            StopCoroutine(InvisibleCoroutine());

        if (_notAttackedCoroutine != null)
            StopCoroutine(NotAttackedCoroutine());
    }

    private IEnumerator UseCoroutine()
    {
        _searchingCollider.radius = _maxDistanceVisible;
        PayCost();
        _useInvisibleCoroutine = StartCoroutine(InvisibleCoroutine());
        yield return null;
    }

    private IEnumerator InvisibleCoroutine()
    {
        yield return _notAttackedCoroutine = StartCoroutine(NotAttackedCoroutine());

        Collider2D hitEnemy = Physics2D.OverlapCircle(transform.position, _maxDistanceVisible, _enemyLayerMask);
        Debug.Log("HitEnemy == " + hitEnemy);
        if (hitEnemy == null)
        {
            Debug.Log("HitEnemy == null");
            _enemyIsSees = false;
            if (!_enemyIsSees && !_isUsing && !_isAttacked)
            {
                _isUsing = true;
                _playerLinks.CharacterState.AddState(new InvisibleState(), Mathf.Infinity, 0, States.Invisible);
                // уменьшаем скорость передвижения на 30%
                moveSpeedDecrease += reduceMoveSpeed;
                _playerLinks.Move.ChangeMoveSpeed(moveSpeedDecrease);
                // Увеличиваем реген энергии на 30%
                _playerLinks.Stamina.RegenerationValue *= (1 + _increaseEnergyRegen);
            }
        }
        else if (hitEnemy != null)
        {
            Debug.Log("HitEnemy != null");
            _enemyIsSees = true;
            if (_enemyIsSees && _isUsing || _isAttacked)
            {
                Cancel();
            }
        }
        else
        {
            Debug.Log("else");
            yield return null;
        }
    }

    private IEnumerator NotAttackedCoroutine()
    {
        float time = 0;
        while (time < _timeWithoutDamage)
        {
            Debug.Log("time < _timeWithoutDamage");
            if (_playerHealth.CurrentHealth == _playerHealth.MaxHealth)
            {
                time += Time.deltaTime;
                _isAttacked = false;
                Debug.Log("time < _timeWithoutDamage/ isAttacked == " + _isAttacked);
            }
            else
            {
                _isAttacked = true;
                break;
            }
            yield return null;
        }
    }

    private void ResetAbility()
    {
        if (_isUsing)
        {
            _playerLinks.CharacterState.RemoveState(States.Invisible);
            // 1.1285715f - число, чтобы вернуть скорость к стандартному значению
            moveSpeedDecrease -= reduceMoveSpeed;
            _playerLinks.Move.ChangeMoveSpeed(moveSpeedDecrease);
            // Уменьшаем реген энергии на 30%
            _playerLinks.Stamina.RegenerationValue /= (1 + _increaseEnergyRegen);
            _isUsing = false;
        }
    }
}