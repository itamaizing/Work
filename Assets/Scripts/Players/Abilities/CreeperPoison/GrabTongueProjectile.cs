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
    private CharacterState _targetCharacterState;
    private MoveComponent _targetMoveComponent;

    private Vector3 _startPosition;
    private Vector3 _endPosition;

    private float _moveSpeedDirectionFromPlayer = 0.2f; // speed projectile 0.2 cell per second
    private float _moveSpeedDirectionToPlayer = 1.2f; // speed projectile 0.6 cell per second

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
        _toungeToTargetCoroutine = StartCoroutine(TongueToTarget());
    }

    private IEnumerator TongueToTarget()
    {
        float startTime = Time.time;
        Vector3 currentPosition = _startPosition;

        while (currentPosition != _endPosition)
        {
            float time = (Time.time - startTime) / _moveSpeedDirectionFromPlayer;
            currentPosition = Vector3.Lerp(_startPosition, _endPosition, time);
            _lineRenderer.SetPosition(1, currentPosition);
            yield return null;
        }

        _toungeFromPlayerCoroutine = StartCoroutine(PullTargetToPlayer(_moveSpeedDirectionToPlayer, _startPosition));
    }

    private IEnumerator PullTargetToPlayer(float speed, Vector3 playerPosition)
    {
        float startTime = Time.time;
        float time;
        Vector3 currentPosition = _endPosition;

        while (currentPosition != _startPosition)
        {
            time = (Time.time - startTime) / _moveSpeedDirectionToPlayer;
            currentPosition = Vector3.Lerp(_endPosition, _startPosition, time);

            Vector3 direction = (_target.transform.position - _startPosition).normalized;

            PullTarget(direction, time);

            _lineRenderer.SetPosition(1, currentPosition);
            yield return null;
        }

        DestoryProjectile();
    }

    [Server]
    private void PullTarget(Vector3 direction, float time)
    {
        if (!_targetCharacterState.CheckForState(States.Immateriality))
        {
            _targetCharacterState.AddState(States.Immateriality, time * 1.3f, 0, _player.gameObject, null);
        }

        _targetMoveComponent.TargetRpcDoMove((Vector3)_target.transform.position - direction * time * 1.2f, time);
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
            StopCoroutine(PullTargetToPlayer(_moveSpeedDirectionToPlayer, _startPosition));
            _toungeFromPlayerCoroutine = null;
        }
    }

    public void InitializationProjectile(Character player, Character target, Vector3 startPosition, Vector3 endPosition)
    {
        _player = player;
        _target = target;
        _startPosition = startPosition;
        _endPosition = endPosition;

        _targetMoveComponent = _target.GetComponent<MoveComponent>();
        _targetCharacterState = _target.GetComponent<CharacterState>();

        _lineRenderer.SetPosition(0, _startPosition);
        _lineRenderer.SetPosition(1, _endPosition);
    }
}
