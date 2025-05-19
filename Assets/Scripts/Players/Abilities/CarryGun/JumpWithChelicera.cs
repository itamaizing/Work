using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class JumpWithChelicera : Skill
{
    [SerializeField] private Character _player;
    [SerializeField] private CheliceraStrike _cheliceraeStrike;
    [SerializeField] private float basePsi = 1f;
    [SerializeField] private float _distanceJump;

    private Animator _animator;
    private Character _target;
    private Vector3 _mousePosition = Vector3.positiveInfinity;

    private static readonly int jumpStart = Animator.StringToHash("JumpStart");
    private static readonly int jumpEnd = Animator.StringToHash("JumpEnd");

    private float _delayBeforeJump = 0.3f;
    private float _minDistance = 0.6f;
    private float _additionalDamageInPercentage;

    private bool _isTarget = false;
    private bool _isJumpDone = false;
    bool hasDealtDamage = false;

    public override bool IsPayCostStartCooldown => false;
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
        hasDealtDamage = false;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        _castDeley = _delayBeforeJump;

        if (_target != null)
        {
            TargetInfo targetInfo = new();
            targetInfo.Targets.Add(_target);
            targetInfo.Points.Add(_mousePosition);
            callbackDataSaved(targetInfo);
            yield break;
        }

        while (_target == null && float.IsPositiveInfinity(_mousePosition.x))
        {
            if (GetMouseButton)
            {
                _target = GetRaycastTarget();
                _mousePosition = GetMousePoint();

                if (_target != null)
                {
                    _isTarget = true;
                    _player.Move.LookAtTransform(_target.transform);
                    _isCanCancle = false;
                }
            }
            yield return null;
        }

        TargetInfo info = new();
        info.Targets.Add(_target);
        info.Points.Add(_mousePosition);
        callbackDataSaved(info);
    }

    protected override IEnumerator CastJob()
    {
        if (_isTarget && _target != null) ExecuteJump();

        yield return null;
    }

    private void ExecuteJump()
    {
        if (_target == null) return;

        _isJumpDone = true;

        float distanceToTarget = Vector2.Distance(_target.transform.position, _player.transform.position);
        _additionalDamageInPercentage = 0.1f + (distanceToTarget / 0.1f) * 0.005f;

        Vector3 direction = (_target.transform.position - transform.position).normalized;

        CmdExecuteJump(_player.gameObject, _target.netId, direction, _additionalDamageInPercentage);
    }

    //private float NormalizeDistance(float distance)
    //{
    //    float minDistance = 2.2f;
    //    float maxDistance = 8f;
    //    return Mathf.Clamp((distance - minDistance) / (maxDistance - minDistance) * (_distanceJump - _minDistance) + _minDistance, _minDistance, _distanceJump);
    //}

    private bool CheckCanCast()
    {
        return _target != null && Vector3.Distance(_target.transform.position, transform.position) <= Radius &&
               NoObstacles(_target.transform.position, _obstacle);
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
        Invoke(nameof(ResetBool), 1f);
        HandleJumpEnd();
        ClearData();
        AnimCastEnded();
    }

    public void ApplyRootTrue()
    {
        IncreaseSetCooldown(CooldownTime);
        Hero.Move.CanMove = false;
        _animator.applyRootMotion = true;
    }

    public void JumpEndSpeedAnim()
    {
        float timeDelay = _distanceJump / 10;
        _player.Animator.SetFloat("JumpEndSpeed", 1f / timeDelay);
    }

    private void HandleJumpAnimEnd()
    {
        if (_animator != null)
        {
            float transitionDuration = 0.15f;
            _animator.CrossFade(jumpEnd, transitionDuration);
        }
    }

    public void HandleJumpEnd()
    {
        _animator.applyRootMotion = false;
        _player.Move.StopLookAt();
        Hero.Move.CanMove = true;
        _isCanCancle = true;
    }

    #region Talents

    public void EvolutionTalentOne(bool value)
    {
        MaxChargers = value ? 2 : 1;
    }

    #endregion 

    [Command]
    private void CmdExecuteJump(GameObject player, uint targetNetId, Vector3 direction, float additionalDamage)
    {
        if (!NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity identity)) return;

        Character targetCharacter = identity.GetComponent<Character>(); 
        if (targetCharacter == null) return;

        MoveComponent playerMove = player.GetComponent<MoveComponent>();

        Vector3 jumpPosition = Vector3.MoveTowards(targetCharacter.transform.position, player.transform.position, _minDistance);
        playerMove.TargetRpcDoMove(jumpPosition, _distanceJump / 10);

        StartCoroutine(TrackMovementDuringJumpCoroutine(playerMove, targetCharacter, additionalDamage));
    }


    private IEnumerator TrackMovementDuringJumpCoroutine(MoveComponent playerMove, Character target, float additionalDamage)
    {
        Vector3 lastPlayerPos = playerMove.transform.position;
        Vector3 lastTargetPos = target.transform.position;

        float playerDistanceAccumulator = 0f;
        float targetDistanceAccumulator = 0f;

        bool jumpEndAnimPlayed = false;
        float stopDistance = _minDistance + 0.5f;

        while (Vector3.Distance(playerMove.transform.position, target.transform.position) > stopDistance)
        {
            Vector3 currentPlayerPos = playerMove.transform.position;
            float playerMoved = Vector3.Distance(lastPlayerPos, currentPlayerPos);
            playerDistanceAccumulator += playerMoved;

            while (playerDistanceAccumulator >= 0.1f)
            {
                playerDistanceAccumulator -= 0.1f;
                if (_player != null && _player.TryGetComponent<BasePsionicEnergy>(out var psiEnergy)) psiEnergy.AddAndResetDecay(basePsi);
            }

            lastPlayerPos = currentPlayerPos;

            if (target != null && !target.IsDead)
            {
                Vector3 currentTargetPos = target.transform.position;
                float targetMoved = Vector3.Distance(lastTargetPos, currentTargetPos);
                targetDistanceAccumulator += targetMoved;

                while (targetDistanceAccumulator >= 0.1f)
                {
                    targetDistanceAccumulator -= 0.1f;
                    if (_player != null && _player.TryGetComponent<BasePsionicEnergy>(out var psiEnergy)) psiEnergy.AddAndResetDecay(basePsi);
                }

                lastTargetPos = currentTargetPos;
            }

            yield return null;
        }

        if (!jumpEndAnimPlayed)
        {
            jumpEndAnimPlayed = true;
            RpcHandleJumpAnimEnd();

            if (target != null && !target.IsDead && _cheliceraeStrike != null && _cheliceraeStrike.IsCooldowned && !_cheliceraeStrike.Disactive) RpcCheliceraeStrike(target);
        }
    }


    [ClientRpc]
    private void RpcHandleJumpAnimEnd()
    {
        HandleJumpAnimEnd();
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo == null || targetInfo.Targets == null || targetInfo.Targets.Count == 0 || targetInfo.Targets[0] == null) return;

        _target = (Character)targetInfo.Targets[0];
        _mousePosition = targetInfo.Points[0];
        _isTarget = true;
        _player.Move.LookAtTransform(_target.transform);
        _isCanCancle = false;
    }

    [ClientRpc]
    private void RpcCheliceraeStrike(Character target)
    {
        _cheliceraeStrike.SetTarget(target);
        _cheliceraeStrike.CheliceraStrikeCast();
        _cheliceraeStrike.ClearDataCheliceraStrike();
    }
}