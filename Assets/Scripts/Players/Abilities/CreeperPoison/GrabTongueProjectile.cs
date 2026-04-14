using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class GrabTongueProjectile : NetworkBehaviour
{
    [SerializeField] private LineRenderer _lineRenderer;

    private Character _player;
    private Character _target;
    private CharacterState _targetCharacterState;

    private Vector3 _startPosition;
    private Vector3 _endPosition;

    private float _throwDuration = 0.2f;
    private float _pullDuration = 0.6f;

    private Coroutine _toTargetCoroutine;
    private Coroutine _pullCoroutine;

    private void Awake()
    {
        if (_lineRenderer == null)
            _lineRenderer = GetComponent<LineRenderer>();
    }

    private void Start()
    {
        if (_lineRenderer == null)
        {
            Debug.LogError("LineRenderer missing!", this);
            return;
        }

        _lineRenderer.positionCount = 2;
        _lineRenderer.enabled = true;
    }

    private void Update()
    {
        if (_player == null || _lineRenderer == null) return;
        _lineRenderer.SetPosition(0, _player.transform.position);
    }

    public void Init(Character player, Character target, Vector3 startPosition, Vector3 endPosition)
    {
        _player = player;
        _target = target;
        _startPosition = startPosition;
        _endPosition = endPosition;

        _targetCharacterState = _target.GetComponent<CharacterState>();

        if (_lineRenderer == null)
            _lineRenderer = GetComponent<LineRenderer>();

        _lineRenderer.positionCount = 2;
        _lineRenderer.SetPosition(0, startPosition);
        _lineRenderer.SetPosition(1, startPosition);

        StartTongue();
    }

    private void StartTongue()
    {
        if (_toTargetCoroutine != null)
            StopCoroutine(_toTargetCoroutine);

        _toTargetCoroutine = StartCoroutine(TongueToTarget());
    }

    private IEnumerator TongueToTarget()
    {
        float timer = 0f;

        Vector3 start = _startPosition;
        Vector3 end = _endPosition;

        while (timer < _throwDuration)
        {
            timer += Time.deltaTime;
            float t = timer / _throwDuration;

            Vector3 currentPos = Vector3.Lerp(start, end, t);

            if (_lineRenderer != null)
                _lineRenderer.SetPosition(1, currentPos);

            yield return null;
        }

        if (_lineRenderer != null)
            _lineRenderer.SetPosition(1, end);

        if (isServer)
        {
            if (_pullCoroutine != null)
                StopCoroutine(_pullCoroutine);

            _pullCoroutine = StartCoroutine(PullTargetToPlayer());
        }
    }

    private IEnumerator PullTargetToPlayer()
    {
        if (_target == null) yield break;

        Transform targetTransform = _target.transform;

        Vector3 start = targetTransform.position;
        Vector3 end = _player.transform.position;

        var agent = _target.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled) agent.enabled = false;

        float timer = 0f;

        _target.Move.IsMoveBlocked = true;
        _target.Move.StopMoveAndAnimationMove();

        if (_targetCharacterState != null && !_targetCharacterState.CheckForState(States.Immateriality)) _targetCharacterState.AddState(States.Immateriality, _pullDuration, 0, _player.gameObject, null);

        while (timer < _pullDuration)
        {
            timer += Time.deltaTime;
            float t = timer / _pullDuration;

            Vector3 currentPos = Vector3.Lerp(start, end, t);

            targetTransform.position = currentPos;

            if (_lineRenderer != null)
                _lineRenderer.SetPosition(1, currentPos);

            yield return null;
        }

        targetTransform.position = end;

        if (agent != null && !agent.enabled) agent.enabled = true;

        _target.Move.CancelMoveTowards();
        _target.Move.StopMoveAndAnimationMove();
        _target.Move.IsMoveBlocked = false;

        DestroyProjectile();
    }

    private void DestroyProjectile()
    {
        if (_toTargetCoroutine != null)
        {
            StopCoroutine(_toTargetCoroutine);
            _toTargetCoroutine = null;
        }

        if (_pullCoroutine != null)
        {
            StopCoroutine(_pullCoroutine);
            _pullCoroutine = null;
        }

        Destroy(gameObject);
    }
}