using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabTongueProjectile : NetworkBehaviour
{
    [SerializeField] private LineRenderer _lineRenderer;

    private Character _player;
    private Character _target;

    private Vector3 _startPosition;
    private Vector3 _endPosition;

    private float _moveSpeedDirectionFromPlayer = 20f; // скорость 0.2 клетки в секунду.
    private float _moveSpeedDirectionToPlayer = 6f; // скорость 0.6 клеток в секунду.

    private Coroutine _toungeToTargetCoroutine;
    private Coroutine _toungeFromPlayerCoroutine;

    private void Start()
    {
        _lineRenderer.positionCount = 2;        
    }

    private void Update()
    {
        _lineRenderer.SetPosition(0, _startPosition);
        _lineRenderer.SetPosition(1, _endPosition);
    }

    public void StartTongueAttract()
    {
        Debug.Log("GrabTongueProjectile / StartTongueAttract work");
        _toungeToTargetCoroutine = StartCoroutine(TongueToTarget());
    }

    private IEnumerator TongueToTarget()
    {
        Debug.Log("GrabTongueProjectile / TongueToTarget work");
        float elapsedTime = 0f;
        float distance = Vector2.Distance(_startPosition, _endPosition);
        float duration = distance / _moveSpeedDirectionFromPlayer;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            _endPosition = Vector2.Lerp(_startPosition, _endPosition, elapsedTime / duration);
            yield return null;
        }

        _toungeFromPlayerCoroutine = StartCoroutine(PullTargetToPlayer(_target, _moveSpeedDirectionToPlayer));
    }

    private IEnumerator PullTargetToPlayer(Character target, float speed)
    {
        Debug.Log("GrabTongueProjectile / PullTargetToPlayer work");
        float elapsedTime = 0f;
        Vector3 startPosition = target.transform.position;
        float distance = Vector2.Distance(startPosition, _startPosition);
        float duration = distance / speed;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            Vector3 newPos = Vector2.Lerp(startPosition, _startPosition, elapsedTime / duration);
            target.transform.position = newPos;
            yield return null;
        }

        DestoryProjectile();
    }

    private void DestoryProjectile()
    {
        Destroy(gameObject);

        if (_toungeToTargetCoroutine != null)
        {
            StopCoroutine(TongueToTarget());
            _toungeToTargetCoroutine = null;
        }
        if (_toungeFromPlayerCoroutine != null)
        {
            StopCoroutine(PullTargetToPlayer(_target, _moveSpeedDirectionToPlayer));
            _toungeFromPlayerCoroutine = null;
        }
    }

    public void InitializationProjectile(Character player, Character target, Vector3 startPosition, Vector3 endPosition)
    {
        _player = player;
        _target = target;
        _startPosition = startPosition;
        _endPosition = endPosition;

        _lineRenderer.SetPosition(0, _startPosition);
        _lineRenderer.SetPosition(1, _endPosition);
    }
}
