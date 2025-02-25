using Mirror;
using System.Collections;
using UnityEngine;

public class JumpWithChelicera : Skill
{
    [SerializeField] private Character _player;
    [SerializeField] private CheliceraStrike _cheliceraeStrike;

    [SerializeField] private float _distanceJump;
    [SerializeField] private float _durationJump;

    private Animator _animator;
    private Character _target;
    private Vector3 _mousePosition = Vector3.positiveInfinity;

    private static readonly int jumpStart = Animator.StringToHash("JumpStart");
    private static readonly int jumpEnd = Animator.StringToHash("JumpEnd");

    private float _delayBeforeJump = 0.3f;
    private float _minDistance = 0.1f;
    private float _baseIncreasedDamage = 0.05f;
    private float _maxIncreasedDamage = 0.2f;
    private float _increaseDamageStandingStill = 0.1f;
    private float _additionalDamageInPercentage;

    private bool _isTarget = false;
    private bool _isJumpDone = false;
    protected override int AnimTriggerCast => jumpStart;
    protected override int AnimTriggerCastDelay => 0;
    public bool IsJumpDone { get => _isJumpDone; set => _isJumpDone = value; }

    protected override bool IsCanCast => CheckCanCast();

    private void Start()
    {
        _animator = GetComponent<Animator>();
    }

    protected override void ClearData()
    {
        _target = null; 
        _mousePosition = Vector3.positiveInfinity;
        _isTarget = false;
    }

    protected override IEnumerator PrepareJob()
    {
        _castDeley = _delayBeforeJump;

        while (_target == null && float.IsPositiveInfinity(_mousePosition.x))
        {
            if (GetMouseButton)
            {
                _target = GetRaycastTarget();
                _mousePosition = GetMousePoint();

                if (_target != null)
                {
                    _isTarget = true;
                }
            }
            yield return null;
        }
    }

    protected override IEnumerator CastJob()
    {
        if (_isTarget && _target != null)
        {
            ExecuteJump();
        }
        yield return null;
    }

    private bool CheckCanCast()
    {
        return _target != null && Vector2.Distance(_mousePosition, transform.position) <= Radius &&
               NoObstacles(_mousePosition, _obstacle);
    }

    private void ExecuteJump()
    {
        _isJumpDone = true;

        float distanceToTarget = Vector2.Distance(_target.transform.position, _player.transform.position);
        float normalizedDistance = NormalizeDistance(distanceToTarget);

        _additionalDamageInPercentage = normalizedDistance < _minDistance
            ? _increaseDamageStandingStill
            : Mathf.Clamp(normalizedDistance * _baseIncreasedDamage, _baseIncreasedDamage, _maxIncreasedDamage);

        Vector3 direction = (_target.transform.position - transform.position).normalized;

        CmdExecuteJump(_player.gameObject, _target.gameObject, direction, _additionalDamageInPercentage);

        Invoke(nameof(ResetBool), 1f);
    }

    private float NormalizeDistance(float distance)
    {
        float minDistance = 2.2f;
        float maxDistance = 8f;
        return Mathf.Clamp((distance - minDistance) / (maxDistance - minDistance) * (_distanceJump - _minDistance) + _minDistance, _minDistance, _distanceJump);
    }

    private void ResetBool()
    {
        _isJumpDone = false;
    }

    public void JumpCast()
    {
        AnimStartCastCoroutine();
    }

    public void JumpEnd()
    {
        AnimCastEnded();
    }

    public void ApplyRootTrue()
    {
        Hero.Move.CanMove = false;
        _animator.applyRootMotion = true;
    }

    private void HandleJumpEnd()
    {
        if (_animator != null)
        {
            _animator.ResetTrigger(jumpStart);
            _animator.SetTrigger(jumpEnd);
        }
    }

    private void InvokejumpEnd()
    {
        Hero.Move.CanMove = true;
        _animator.applyRootMotion = false;
    }

    [Command]
    private void CmdExecuteJump(GameObject player, GameObject target, Vector3 direction, float additionalDamage)
    {
        MoveComponent playerMove = player.GetComponent<MoveComponent>();
        Character targetCharacter = target.GetComponent<Character>();

        Vector3 jumpPosition = targetCharacter != null
            ? Vector3.MoveTowards(targetCharacter.transform.position, player.transform.position, _minDistance + 0.5f)
            : player.transform.position + direction * ((_distanceJump - 0.5f) * GlobalVariable.cellSize);

        playerMove.TargetRpcDoMove(jumpPosition, _durationJump);

        RpcHandleJumpEnd();
        DamageDeal(target, additionalDamage);
    }

    [ClientRpc] private void RpcHandleJumpEnd()
    {
        HandleJumpEnd();
        Invoke("InvokejumpEnd", 1f);
    }

    [ClientRpc]
    private void DamageDeal(GameObject target, float additionalDamage)
    {
        _cheliceraeStrike.DealDamage(target, additionalDamage);
    }
}
