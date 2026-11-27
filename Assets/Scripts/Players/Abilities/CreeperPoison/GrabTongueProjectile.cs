using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class GrabTongueProjectile : NetworkBehaviour
{
    [SerializeField] private LineRenderer _lineRenderer;

    private Character _player;
    private Transform _origin;
    private Character _target;
    private CharacterState _targetCharacterState;

    private Vector3 _startPosition;
    private Vector3 _endPosition;

    private float _throwDuration = 0.2f;
    private float _pullDuration = 0.6f;
    private const float PullDistance = 3f;
    private bool _canPull;

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

    private void LateUpdate()
    {
        if (_player == null || _lineRenderer == null) return;
        _lineRenderer.SetPosition(0, _origin != null ? _origin.position : _player.transform.position);
    }

    public void Init(Character player, Character target, Vector3 endPosition, bool canPull, float throwDuration, float pullDuration)
    {
        _player = player;
        _target = target;
        _canPull = canPull;
        _throwDuration = Mathf.Max(0.01f, throwDuration);
        _pullDuration = Mathf.Max(0.01f, pullDuration);
        _origin = player.transform;
        if (player.AttachmentPoints != null && player.AttachmentPoints.TryGetPoint(AttachmentPointType.Head, out Transform head))
            _origin = head;
        _startPosition = _origin.position;
        _endPosition = endPosition;

        _targetCharacterState = _target.GetComponent<CharacterState>();

        if (_lineRenderer == null)
            _lineRenderer = GetComponent<LineRenderer>();

        _lineRenderer.positionCount = 2;
        _lineRenderer.SetPosition(0, _startPosition);
        _lineRenderer.SetPosition(1, _startPosition);

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
            if (!_canPull || _target == null || _target.IsDead)
            {
                DestroyProjectile();
                yield break;
            }

            if (_pullCoroutine != null)
                StopCoroutine(_pullCoroutine);

            _pullCoroutine = StartCoroutine(PullTargetToPlayer());
        }
    }

    private IEnumerator PullTargetToPlayer()
    {
        if (!_canPull || _target == null) yield break;

        Transform targetTransform = _target.transform;

        Vector3 start = targetTransform.position;
        Vector3 end = Vector3.MoveTowards(start, _player.transform.position, PullDistance);

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

        SynchronizeTargetPosition(end);

        DestroyProjectile();
    }

    private void SynchronizeTargetPosition(Vector3 position)
    {
        _target.transform.position = position;
        foreach (var networkTransform in _target.GetComponents<NetworkTransformBase>())
        {
            if (!networkTransform.enabled || networkTransform.target != _target.transform) continue;

            networkTransform.ResetState();
            networkTransform.RpcTeleport(position);
        }
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

        NetworkServer.Destroy(gameObject);
    }
}
