using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class Invisible : Ability
{
    [SerializeField] private Character _playerLinks;
    [SerializeField] private CharacterData _playerData;
    [SerializeField] private LayerMask _enemyLayerMask;

    [SerializeField] private float _reduceMoveSpeed = 0.3f;
    [SerializeField] private float _moveSpeedDecrease;
    [SerializeField] private float _originalMoveSpeed;

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

    private void Update()
    {
        if (_useInvisibleCoroutine != null) 
        {
            if (_playerHealth.CurrentHealth != _playerHealth.MaxHealth)
                Cancel();
        }
    }

    protected override void Cast()
    {
        _useJob = StartCoroutine(UseCoroutine());
    }

    protected override void Cancel()
    {
        _playerLinks.CharacterState.RemoveState(States.Invisible);
        ResetAbility();

        if (_useJob != null)
            StopCoroutine(UseCoroutine());

        if (_useInvisibleCoroutine != null)
            StopCoroutine(InvisibleCoroutine());

        if (_notAttackedCoroutine != null)
            StopCoroutine(NotAttackedCoroutine());
    }

    private IEnumerator UseCoroutine()
    {
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
                InvisibleState();
            }
        }
        else if (hitEnemy != null)
        {
            Debug.Log("HitEnemy != null");
            _enemyIsSees = true;

            if (_enemyIsSees && _isUsing)
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

    private void InvisibleState()
    {
        CmdInvisibleState();
    }

    private void ResetAbility()
    {
        CmdResetAbility();
    }

    [Command]
    private void CmdResetAbility()
    {
        if (_isUsing)
        {
            // Возвращаем скорость к изначальной
            _playerLinks.Move.SetDefaultSpeed();

            // Уменьшаем реген энергии на 30%
            _playerLinks.Stamina.RegenerationValue /= (1 + _increaseEnergyRegen);
            _isUsing = false;
        }
    }

    [Command]
    private void CmdInvisibleState()
    {
        if (!_playerLinks.CharacterState.CheckForState(States.Invisible))
        {
            Debug.Log("CheckState");
            _playerLinks.CharacterState.CmdAddState(States.Invisible, Mathf.Infinity, 0);
        }
        // уменьшаем скорость передвижения на 30%
        _moveSpeedDecrease = _originalMoveSpeed * _reduceMoveSpeed;

        float _applyModifiedMoveSpeed = _originalMoveSpeed - _moveSpeedDecrease;

        _playerLinks.Move.ChangeMoveSpeed(_applyModifiedMoveSpeed);
        Debug.Log("speed == " + _playerLinks.Move._agent.maxSpeed);

        // Увеличиваем реген энергии на 30%
        _playerLinks.Stamina.RegenerationValue *= (1 + _increaseEnergyRegen);
        Debug.Log("Stamina regen == " + _playerLinks.Stamina.RegenerationValue);
    }

}