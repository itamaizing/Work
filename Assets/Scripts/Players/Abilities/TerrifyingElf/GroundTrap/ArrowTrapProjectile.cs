using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ArrowTrapProjectile : Projectiles
{
    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _lifeTime = 10f;
    [SerializeField] private bool _selfDestroyInEndPoint = true;
    
    private Vector3 _targetPosition;
    private bool _isFlyingToTarget = false;

    private void Update()
    {
        if (_isFlyingToTarget)
        {
            FlyTowardsTarget();
        }
    }

    public void StartFly(Vector3 targetPosition)
    {
        _targetPosition = targetPosition;
        _isFlyingToTarget = true;
        Destroy(gameObject, _lifeTime);
    }

    private void FlyTowardsTarget()
    {
        Vector3 direction = (_targetPosition - transform.position).normalized;
        float distanceThisFrame = _speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, _targetPosition) <= distanceThisFrame)
        {
            transform.position = _targetPosition;
            _isFlyingToTarget = false;
            CmdOnEndPointReached();
        }
        else
        {
            transform.position += direction * distanceThisFrame;
        }
    }

    [Command]
    private void CmdOnEndPointReached()
    {
        if (_selfDestroyInEndPoint) Destroy(gameObject);
    }
}
