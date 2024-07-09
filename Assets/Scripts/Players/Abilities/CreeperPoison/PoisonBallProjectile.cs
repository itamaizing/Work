using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonBallProjectile : NetworkBehaviour
{
    [SerializeField] protected GameObject _hitEffect;
    [SerializeField] protected SpriteRenderer _spriteRenderer;
    [SerializeField] protected Collider2D _collider;
    [SerializeField] private Rigidbody2D _rbBall;
    [SerializeField] private Transform _dad;

    private Vector3 _point;
    private Vector3 _startPosition;
    private GameObject _target;

    private float _energyDad;
    private float _fastMovementSpeed = 0.6f;
    private float _slowMovementSpeed = 1.7f;
    private float _durationStun = 1.2f;
    private float _rangePush = 1.0f;
    private float _maxDistance = 6f;
    private float _currentDamage = 35f;

    private bool _isFast;

    private void Start()
    {
        _startPosition = transform.position;
    }

    [Server]
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("OnTrigger PoisonBallProjectile");
        if (collision.gameObject.transform != _dad)
        {
            if (collision.TryGetComponent<HealthComponent>(out var targetHealth))
            {
                DealDamage(targetHealth, _currentDamage, DamageType.Magical, AttackRangeType.RangeAttack);
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

    public void MoveBallToPoint(Vector3 point, bool isFast)
    {
        Debug.Log("PoisonBallProjectile MoveBallToPoint");

        float _speed = isFast ? _fastMovementSpeed : _slowMovementSpeed;

        CmdMovingToPoint(point, _speed);
    }

    private void CmdMovingToTarget(Vector3 target, float speed)
    {
        Debug.Log("PoisonBallProjectile MovingToTarget");

        _rbBall.DOMove(target, speed * _maxDistance / GlobalVariable.cellSize).SetEase(Ease.Linear).OnComplete(DestroyProjectile);
    }

    private void CmdMovingToPoint(Vector3 point, float speed)
    {
        Debug.Log("PoisonBallProjectile MovingToPoint _point == " + point);

        _rbBall.DOMove(point, speed * _maxDistance / GlobalVariable.cellSize).SetEase(Ease.Linear).OnComplete(DestroyProjectile);
    }
    #endregion

    #region Making Damage
    private void DealDamage(HealthComponent targetHealth, float currentDamage, DamageType damageType, AttackRangeType attackRangeType)
    {
        Energy _energyLink = (Energy)_dad.GetComponent<Character>().Stamina;
        _energyLink.SumDamageMake(currentDamage);

        targetHealth.TryTakeDamage(currentDamage, damageType, attackRangeType);

        PushEnemy(targetHealth.gameObject);

        Destroy(this.gameObject);
    }

    private void PushEnemy(GameObject target)
    {
        Vector2 directionPush = (target.transform.position - transform.position).normalized;

        float _durationPush = _durationStun;
        float _rangePush = this._rangePush;

        _durationPush = ((_durationPush * GlobalVariable.cellSize) * _rangePush) / GlobalVariable.cellSize;
        target.GetComponent<Transform>().transform.DOMove((Vector2)target.transform.position + directionPush, _durationPush).SetEase(Ease.Linear);

        target.GetComponent<CharacterState>().AddState(new StunnedState(), _durationStun, _durationPush, States.Stun);
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
