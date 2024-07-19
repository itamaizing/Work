using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabTongueProjectile : NetworkBehaviour
{
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private LineRenderer _tongueLineRendrer;
    private Character _dad;

    private float _moveSpeedDirectionFromPlayer = 20f; // скорость 0.2 клетки в секунду.
    private float _moveSpeedDirectionToPlayer = 6f; // скорость 0ю6 клеток в секунду.

    [SyncVar] private Vector2 _startPosition;
    [SyncVar] private Vector2 _endPosition;

    private GameObject _target;

    private void Start()
    {
        _tongueLineRendrer.positionCount = 2;
    }

    private void Update()
    {
        _tongueLineRendrer.SetPosition(0, _startPosition);
        _tongueLineRendrer.SetPosition(1, _endPosition);
    }
    
    private void PulledTarget(GameObject target)
    {
        Debug.Log("Pulled target work");
        Vector2 directionPulled = target.transform.position - _dad.transform.position;

        target.GetComponent<Transform>().DOMove((Vector2)target.transform.position - directionPulled, 0.6f).SetEase(Ease.Linear);
        StartCoroutine(ReturnTongue(target.transform.position));
    }

    public void MovingTongueFromPlayer(Vector2 startPosition, Vector2 endPosition)
    {
        _startPosition = startPosition;
        _endPosition = endPosition;

        _tongueLineRendrer.SetPosition(0, startPosition);
        _tongueLineRendrer.SetPosition(1, endPosition);

        RaycastHit2D hit = Physics2D.Linecast(startPosition, endPosition, _enemyLayer);
        if (hit.collider != null && hit.collider.transform != _dad)
        {
            Debug.Log("Hit detected: " + hit.collider.gameObject.name);
            if (hit.collider.TryGetComponent<HealthComponent>(out var target))
            {
                _target = target.gameObject;
                if (_target.gameObject.GetComponent<Character>().CharacterState.CheckForState(States.InAir))
                {
                    _target.GetComponent<MoveComponent>().CanMove = false;
                    StartCoroutine(AnimateTongueWithTarget(startPosition, hit.point, target.gameObject));
                }
            }
        }
        else
        {
            StartCoroutine(AnimateTongueWithoutTarget(startPosition, endPosition));
        }
    }

    private IEnumerator AnimateTongueWithoutTarget(Vector2 startPosition, Vector2 endPosition)
    {
        float elapsedTime = 0f;
        float distance = Vector2.Distance(startPosition, endPosition);
        float duration = distance / _moveSpeedDirectionFromPlayer;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            _endPosition = Vector2.Lerp(startPosition, endPosition, elapsedTime / duration);
            yield return null;
        }

        _endPosition = endPosition;
        DestoryProjectile();
    }

    private IEnumerator AnimateTongueWithTarget(Vector2 startPosition, Vector2 hitPosition, GameObject target)
    {
        float elapsedTime = 0f;
        float distance = Vector2.Distance(startPosition, hitPosition);
        float duration = distance / _moveSpeedDirectionFromPlayer;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            _endPosition = Vector2.Lerp(startPosition, hitPosition, elapsedTime / duration);
            yield return null;
        }

        _endPosition = hitPosition;
        PulledTarget(target);
    }

    private IEnumerator ReturnTongue(Vector2 targetPosition)
    {
        float elapsedTime = 0f;
        float distance = Vector2.Distance(targetPosition, _startPosition);
        float duration = distance / _moveSpeedDirectionToPlayer;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            _endPosition = Vector2.Lerp(targetPosition, _startPosition, elapsedTime / duration);
            yield return null;
        }

        _endPosition = _startPosition;
        DestoryProjectile();
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
