using Mirror;
using System.Collections;
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

    private int _teamIndex;

    private float _moveSpeedDirectionFromPlayer = 0.2f;
    private float _moveSpeedDirectionToPlayer = 0.6f;

    private bool _isPlayerInvisible;
    private bool _isAlly;

    private Coroutine _toungeToTargetCoroutine;
    private Coroutine _toungeFromPlayerCoroutine;

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

        if (isServer && _isPlayerInvisible)
        {
            RpcNewTransparencySprite();
        }
    }

    private void Update()
    {
        if (_lineRenderer == null || _player == null) return;

        _lineRenderer.SetPosition(0, _player.transform.position);
    }

    public void InitializationProjectile(Character player, Character target, Vector3 startPosition, Vector3 endPosition, bool isPlayerInvisible)
    {
        _player = player;
        _target = target;
        _startPosition = startPosition;
        _endPosition = endPosition;

        _isPlayerInvisible = isPlayerInvisible;

        _targetMoveComponent = _target.GetComponent<MoveComponent>();
        _targetCharacterState = _target.GetComponent<CharacterState>();

        if (_lineRenderer == null)
            _lineRenderer = GetComponent<LineRenderer>();

        _lineRenderer.positionCount = 2;
        _lineRenderer.SetPosition(0, _startPosition);
        _lineRenderer.SetPosition(1, _startPosition);

        StartTongueAttract();
    }

    public void StartTongueAttract()
    {
        if (_toungeToTargetCoroutine != null)
            StopCoroutine(_toungeToTargetCoroutine);

        _toungeToTargetCoroutine = StartCoroutine(TongueToTarget());
    }

    private IEnumerator TongueToTarget()
    {
        float timer = 0f;

        Vector3 start = _startPosition;
        Vector3 end = _endPosition;

        while (timer < 1f)
        {
            timer += Time.deltaTime / _moveSpeedDirectionFromPlayer;

            Vector3 currentPosition = Vector3.Lerp(start, end, timer);

            if (_lineRenderer != null) _lineRenderer.SetPosition(1, currentPosition);

            yield return null;
        }

        if (_lineRenderer != null) _lineRenderer.SetPosition(1, end);

        _toungeFromPlayerCoroutine = StartCoroutine(PullTargetToPlayer());
    }

    private IEnumerator PullTargetToPlayer()
    {
        if (_target == null) yield break;

        float timer = 0f;

        Vector3 start = _target.transform.position;
        Vector3 end = _player.transform.position;

        if (_targetMoveComponent != null) _targetMoveComponent.IsMoveBlocked = true;

        if (_targetCharacterState != null && !_targetCharacterState.CheckForState(States.Immateriality))
        {
            _targetCharacterState.AddState(States.Immateriality, _moveSpeedDirectionToPlayer * 1.3f, 0, _player.gameObject, null);
        }

        while (timer < 1f)
        {
            timer += Time.deltaTime / _moveSpeedDirectionToPlayer;

            Vector3 currentPos = Vector3.Lerp(start, end, timer);

            if (_targetMoveComponent != null)
                _targetMoveComponent.TargetRpcSetTransformPosition(currentPos);

            if (_lineRenderer != null)
                _lineRenderer.SetPosition(1, currentPos);

            yield return null;
        }

        if (_targetMoveComponent != null)
        {
            _targetMoveComponent.TargetRpcSetTransformPosition(end);
            _targetMoveComponent.TargetRpcStopMoveAndAnimationMove();
            _targetMoveComponent.IsMoveBlocked = false;
        }

        DestroyProjectile();
    }

    private void DestroyProjectile()
    {
        if (_toungeToTargetCoroutine != null)
        {
            StopCoroutine(_toungeToTargetCoroutine);
            _toungeToTargetCoroutine = null;
        }

        if (_toungeFromPlayerCoroutine != null)
        {
            StopCoroutine(_toungeFromPlayerCoroutine);
            _toungeFromPlayerCoroutine = null;
        }

        Destroy(gameObject);
    }

    [ClientRpc]
    private void RpcNewTransparencySprite()
    {
        if (_lineRenderer == null) return;

        var localPlayer = NetworkClient.connection.identity.GetComponent<UserNetworkSettings>();
        _isAlly = localPlayer.TeamIndex == _teamIndex;

        Color start = _lineRenderer.startColor;
        Color end = _lineRenderer.endColor;

        float alpha = _isAlly ? 0.5f : 0f;

        start.a = alpha;
        end.a = alpha;

        _lineRenderer.startColor = start;
        _lineRenderer.endColor = end;
    }
}