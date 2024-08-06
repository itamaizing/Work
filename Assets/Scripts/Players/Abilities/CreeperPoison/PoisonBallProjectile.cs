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

    private float _distancePush = 1.0f;
    private float _maxDistance = 6f;
    private float _durationPush = 1.2f;
    private float _durationStun = 1.0f;

    private float _currentDamageForPoisonBall = 35f;

    private GameObject _currentTarget;
    private FootInstincts _footInstincts;
    private void Start()
    {
        _durationPush = 1.0f;
        InitializationComponentsForCountProjectile();
    }

    private void InitializationComponentsForCountProjectile()
    {
        _poisonBall = _dad.GetComponentInChildren<PoisonBall>();

        _footInstincts = _poisonBall.GetComponent<PoisonBall>().FootInstinctsTalent;
        Debug.Log("FootInstincts in PoisonBallProjectile == " + _footInstincts);
        Debug.Log("FootInstincts.isActive in PoisonBallProjectile == " + _footInstincts.isActive);

        _countProjectiles = _poisonBall.GetComponent<PoisonBall>().CountProjectiles;
        _currentTarget = _poisonBall.GetComponent<PoisonBall>().CurrentTarget;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.transform != _dad)
        {
            if (collision.TryGetComponent<HealthComponent>(out var targetHealth))
            {
                DealDamage(targetHealth, _currentDamageForPoisonBall, DamageType.Magical, AttackRangeType.RangeAttack);
                if (_footInstincts.isActive)
                {
                    _footInstincts.ReductionCooldownLightningMovement();
                }
                _poisonBall.LastTarget = targetHealth.gameObject;
                Destroy(gameObject);
            }
        }
    }

    #region MovementBall

    public void MoveBallToTarget(Vector3 target, bool isFast)
    {
        float speed = isFast ? _fastMovementSpeed : _slowMovementSpeed;

        _rbBall.DOMove(target, speed * _maxDistance / GlobalVariable.cellSize).SetEase(Ease.Linear).OnComplete(DestroyProjectile);
    }

    public void MoveBallOnMaxDistance(Vector3 point, bool isFast)
    {
        float speed = isFast ? _fastMovementSpeed : _slowMovementSpeed;

        Vector3 direction = point - transform.position;

        _rbBall.DOMove(transform.position + direction * _maxDistance, speed * _maxDistance / GlobalVariable.cellSize).SetEase(Ease.Linear).OnComplete(DestroyProjectile);
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
        distancePush = 1.0f;
        if (_countProjectiles == 3 && _currentTarget == _poisonBall.LastTarget)
        {
            distancePush = 4.0f;
        }
        PushEnemy(target.gameObject, durationPush, distancePush);
    }

    private void PushEnemy(GameObject target, float durationPush, float distancePush)
    {
        Vector2 directionPush = (target.transform.position - transform.position);

        distancePush = ((distancePush * GlobalVariable.cellSize) * durationPush) / GlobalVariable.cellSize;

        if (_countProjectiles < 3)
        {
            target.GetComponent<Transform>().transform.DOMove((Vector2)target.transform.position - directionPush * distancePush, durationPush).SetEase(Ease.Linear);
        }
        else if (_countProjectiles == 3 && _currentTarget == _poisonBall.LastTarget)
        {
            target.GetComponent<Transform>().transform.DOMove((Vector2)target.transform.position + directionPush * distancePush, durationPush).SetEase(Ease.Linear);
        }
        else if (_countProjectiles == 3 && _currentTarget != _poisonBall.LastTarget)
        {
            target.GetComponent<Transform>().transform.DOMove((Vector2)target.transform.position - directionPush * distancePush, durationPush).SetEase(Ease.Linear);
        }

        target.GetComponent<Character>().CharacterState.AddState(new InAirState(), _durationStun, 0, States.InAir);
    }

    #endregion

    #region InitializationProjectiles

    public void InitializationProjectileForPoisonBall(Transform dad, float energyDad)
    {
        _dad = dad;
        _energyDad = energyDad;
    }

    #endregion

    private void DestroyProjectile()
    {
        Destroy(gameObject);
    }
}