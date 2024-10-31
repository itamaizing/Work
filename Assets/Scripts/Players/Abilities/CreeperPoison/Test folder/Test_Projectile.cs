using DG.Tweening;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Test_Projectile : NetworkBehaviour
{
    [Header("Projectile Base Parameters")]
    [SerializeField] protected MeshRenderer _projectileRenderer;
    [SerializeField] protected Collider _projectileCollider;
    [SerializeField] protected Rigidbody _projectileRigidbody;
    [SerializeField] protected float _speed;
    [SerializeField] protected float _maxDistanceFlying;

    protected Damage _baseDamage = new Damage();

    protected Character _player;
    protected Character _target;

    public abstract void DamageDeal();
    
    public void MoveToPoint(Vector3 point, float speed)
    {
        _maxDistanceFlying *= GlobalVariable.cellSize;

        Vector3 direction = (point - _player.transform.position).normalized;

        Vector3 finalPoint = _player.transform.position + direction * _maxDistanceFlying;

        _projectileRigidbody.DOMove(finalPoint, (_maxDistanceFlying * speed)).SetEase(Ease.Linear).OnComplete(DestroyProjectile);
    }

    public void MoveToTarget(Vector3 targetPos, float speed)
    {
        _maxDistanceFlying *= GlobalVariable.cellSize;

        _projectileRigidbody.DOMove(targetPos, (_maxDistanceFlying * speed)).SetEase(Ease.Linear).OnComplete(DestroyProjectile);
    }

    public void DestroyProjectile()
    {
        Destroy(gameObject);
        _target = null;
    }

}
