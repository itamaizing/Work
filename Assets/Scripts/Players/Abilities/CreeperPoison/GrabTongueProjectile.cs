using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabTongueProjectile : NetworkBehaviour
{
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private SpriteRenderer _tongueSprite;
    private Character _dad;

    private float _moveSpeedDirectionFromPlayer = 20f; // скорость 0.2 клетки в секунду.
    private float _moveSpeedDirectionToPlayer = 6f; // скорость 0ю6 клеток в секунду.

    private Vector2 _startPosition;
    private Vector2 _endPosition;

    private GameObject _target;
    
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

        StartCoroutine(MoveTongue(startPosition, endPosition));
    }

    private IEnumerator MoveTongue(Vector2 startPosition, Vector2 endPosition)
    {
        float elapsedTime = 0f;
        float distance = Vector2.Distance(startPosition, endPosition);
        float duration = distance / _moveSpeedDirectionFromPlayer;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            transform.position = Vector2.Lerp(startPosition, endPosition, elapsedTime / duration);
            yield return null;
        }

        transform.position = endPosition;

        RaycastHit2D hit = Physics2D.Linecast(startPosition, endPosition, _enemyLayer);
        if (hit.collider != null && hit.collider.transform != _dad)
        {
            if (hit.collider.TryGetComponent<HealthComponent>(out var target))
            {
                _target = target.gameObject;
                if (_target.GetComponent<Character>().CharacterState.CheckForState(States.InAir))
                {
                    _target.GetComponent<MoveComponent>().CanMove = false;
                    yield return StartCoroutine(ReturnTongueWithTarget(_target.transform.position));
                }
            }
        }
        else
        {
            yield return StartCoroutine(ReturnTongue(startPosition));
        }

        Destroy(gameObject);
    }

    private IEnumerator ReturnTongue(Vector2 targetPosition)
    {
        float elapsedTime = 0f;
        float distance = Vector2.Distance(targetPosition, _startPosition);
        float duration = distance / _moveSpeedDirectionToPlayer;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            transform.position = Vector2.Lerp(targetPosition, _startPosition, elapsedTime / duration);
            yield return null;
        }

        transform.position = _startPosition;
    }

    private IEnumerator ReturnTongueWithTarget(Vector2 targetPosition)
    {
        Vector2 directionPulled = _target.transform.position - _dad.transform.position;

        _target.transform.DOMove((Vector2)_target.transform.position - directionPulled, 0.6f).SetEase(Ease.Linear);

        float elapsedTime = 0f;
        float distance = Vector2.Distance(targetPosition, _startPosition);
        float duration = distance / _moveSpeedDirectionToPlayer;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            transform.position = Vector2.Lerp(targetPosition, _startPosition, elapsedTime / duration);
            yield return null;
        }

        transform.position = _startPosition;
    }

    public void InitializationProjectile(Character dad)
    {
        _dad = dad;
    }
}
