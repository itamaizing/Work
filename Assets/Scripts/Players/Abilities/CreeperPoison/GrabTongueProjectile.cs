using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabTongueProjectile : NetworkBehaviour
{
    [SerializeField] private LineRenderer _tongueLineRendrer;
    [SerializeField] private LayerMask _enemyLayer;
    private Character _dad;

    private float _distancePulled = 3.0f;
    private float _moveSpeedDirectionFromPlayer = 0.2f;
    private float _moveSpeedDirectionToPlayer = 0.6f;

    private Vector2 _startPos;
    private Vector2 _endPos;

    private void Start()
    {
        _tongueLineRendrer.positionCount = 2;
    }

    private void Update()
    {
        _tongueLineRendrer.SetPosition(0, _startPos);
        _tongueLineRendrer.SetPosition(1, _endPos);
    }
    
    private void PulledTarget(GameObject target)
    {
        Debug.Log("Pulled target work");
        Vector2 directionPulled = target.transform.position - _dad.transform.position;

        float distancePulled = ((_distancePulled * GlobalVariable.cellSize) * _moveSpeedDirectionToPlayer) / GlobalVariable.cellSize;

        target.GetComponent<Transform>().DOMove((Vector2)target.transform.position - directionPulled * _moveSpeedDirectionToPlayer, distancePulled).SetEase(Ease.Linear).OnComplete(DestoryProjectile);
    }

    public void MovingTongueFromPlayer(Vector2 startPosition, Vector2 endPosition)
    {
        _startPos = startPosition;
        _endPos = endPosition;

        _tongueLineRendrer.SetPosition(0, startPosition);
        _tongueLineRendrer.SetPosition(1, endPosition);

        Vector2 directionMoving = endPosition - startPosition;

        float distancePulled = ((_distancePulled * GlobalVariable.cellSize) * _moveSpeedDirectionFromPlayer) / GlobalVariable.cellSize;

        RaycastHit2D hit = Physics2D.Linecast(startPosition, endPosition, _enemyLayer);
        if (hit.collider != null && hit.collider.transform != _dad)
        {
            Debug.Log("Hit detected: " + hit.collider.gameObject.name);
            if (hit.collider.TryGetComponent<HealthComponent>(out var target))
            {
                if (target.IsInAir)
                {
                    PulledTarget(target.gameObject);
                }
            }
        }

        transform.DOMove(endPosition, distancePulled).SetEase(Ease.Linear);
    }

    private void DestoryProjectile()
    {
        Destroy(gameObject);
    }

    public void InitializationProjectile(Character dad)
    {
        _dad = dad;
    }
}
