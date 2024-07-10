using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonBallProjectile : NetworkBehaviour
{
    [SerializeField] protected PoisonBall _poisonBall;
    [SerializeField] protected GameObject _hitEffect;
    [SerializeField] protected SpriteRenderer _spriteRenderer;
    [SerializeField] protected Collider2D _collider;
    [SerializeField] private Rigidbody2D _rbBall;
    [SerializeField] private Transform _dad;

    private int _countProjectiles;

    private float _energyDad;
    private float _fastMovementSpeed = 0.6f;
    private float _slowMovementSpeed = 1.7f;
    private float _durationStun = 1.2f;
    private float _distancePush = 1.0f;
    private float _maxDistance = 6f;
    private float _currentDamage = 35f;
    private float _durationPush;

    private GameObject _currentTarget;
    private GameObject _lastTarget;

    private void Start()
    {
        _durationPush = 1.0f;
        CountProjectile();
    }

    private void CountProjectile()
    {
        _poisonBall = _dad.GetComponentInChildren<PoisonBall>();
        _countProjectiles = _poisonBall.GetComponent<PoisonBall>().CountProjectiles;
        _currentTarget = _poisonBall.GetComponent<PoisonBall>().CurrentTarget;
    }

    [Server]
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.transform != _dad)
        {
            if (collision.TryGetComponent<HealthComponent>(out var targetHealth))
            {
                DealDamage(targetHealth, _currentDamage, DamageType.Magical, AttackRangeType.RangeAttack);
                _poisonBall.LastTarget = targetHealth.gameObject;
                Destroy(gameObject);
            }
        }
    }

    #region MovementBall
    public void MoveBallToTarget(Vector3 target, bool isFast)
    {
        Debug.Log("PoisonBallProjectile MoveBallToTarget");

        float _speed = isFast ? _fastMovementSpeed : _slowMovementSpeed;

        CmdMovingToTarget(target, _speed);
    }

    private void CmdMovingToTarget(Vector3 target, float speed)
    {
        Debug.Log("PoisonBallProjectile MovingToTarget");

        _rbBall.DOMove(target, speed * _maxDistance / GlobalVariable.cellSize).SetEase(Ease.Linear).OnComplete(DestroyProjectile);
    }
    #endregion

    #region Making Damage
    private void DealDamage(HealthComponent targetHealth, float currentDamage, DamageType damageType, AttackRangeType attackRangeType)
    {
        Energy _energyLink = (Energy)_dad.GetComponent<Character>().Stamina;
        _energyLink.SumDamageMake(currentDamage);

        targetHealth.TryTakeDamage(currentDamage, damageType, attackRangeType);

        PushEnemyDependingOnCountProjectile(targetHealth, _durationPush, _distancePush);

        Destroy(this.gameObject);
    }

    private void PushEnemyDependingOnCountProjectile(HealthComponent target, float durationPush, float distancePush)
    {
        if (_countProjectiles < 3)
        {
            PushEnemy(target.gameObject, durationPush, distancePush);
        }
        else if (_countProjectiles == 3)
        {
            if (_currentTarget == _poisonBall.LastTarget)
            {
                distancePush = 4.0f;
            }
            else
            {
                distancePush = 1.0f;
            }
            PushEnemy(target.gameObject, durationPush, distancePush);
        }
    }

    private void PushEnemy(GameObject target, float durationPush, float distancePush)
    {
        Vector2 directionPush = (target.transform.position - transform.position);

        distancePush = ((distancePush * GlobalVariable.cellSize) * durationPush) / GlobalVariable.cellSize;
        target.GetComponent<Transform>().transform.DOMove((Vector2)target.transform.position + directionPush * distancePush, durationPush).SetEase(Ease.Linear);

        //target.GetComponent<CharacterState>().AddState(new StunnedState(), durationStun, 0, States.Stun);
    }
    #endregion

    public void InitializationProjectile(Transform dad, float energyDad)
    {
        _dad = dad;
        _energyDad = energyDad;
    }

    private void DestroyProjectile()
    {
        Destroy(gameObject);
    }
}
