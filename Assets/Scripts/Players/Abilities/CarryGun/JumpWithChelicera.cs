using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class JumpWithChelicera : Skill
{
    [SerializeField] private Character _player;
    [SerializeField] private CheliceraStrike _cheliceraeStrike;
    [SerializeField] private ClawStrike clawStrike;
    [SerializeField] private CooldownEnergy cooldownEnergy;
    [SerializeField] private float basePsi = 1f;
    [SerializeField] private float distanceJump;

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
    private bool _hasDealtDamage = false;
    private bool _isCheliceraStrikeCast = false;


    public override bool IsPayCostStartCooldown => false;
    protected override int AnimTriggerCast => jumpStart;
    protected override int AnimTriggerCastDelay => 0;

    public bool IsJumpDone { get => _isJumpDone; set => _isJumpDone = value; }
    public bool IsCheliceraStrikeCast { get => _isCheliceraStrikeCast; set => _isCheliceraStrikeCast = value; }

    protected override bool IsCanCast => CheckCanCast();

    #region Talent
    private bool isJumpWithCheliceraChanceDamageCrit = false;

    public void JumpWithCheliceraChanceDamageCrit(bool value) => isJumpWithCheliceraChanceDamageCrit = value;
    #endregion

    private void Start() => _animator = GetComponent<Animator>();
    private void OnDestroy() => Canceled -= HandleJumpWithCheliceraEnd;
    private void OnEnable() => Canceled += HandleJumpWithCheliceraEnd;

    protected override void ClearData()
    {
        _target = null;
        _mousePosition = Vector3.positiveInfinity;
        _isTarget = false;
        _hasDealtDamage = false;
    }

    public void JumpWithCheliceraAnimationMove()
    {
        if (_hero == null || _hero.Move == null) return;

        _hero.Move.StopMoveAndAnimationMove();
        _hero.Move.CanMove = false;

        Vector3 direction = _mousePosition - _hero.transform.position;
        bool badDirection = float.IsInfinity(_mousePosition.x) || direction.sqrMagnitude < 0.0001f;

        if (badDirection)
        {
            _hero.Move.StopLookAt();
            return;
        }

        _hero.Move.LookAtPosition(_mousePosition);
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
        if (_isTarget && _target != null)
        {
            cooldownEnergy.CastCooldownEnergySkill(CooldownTime, this);
            ExecuteJump();
        }

        yield return null;
    }

    private void ExecuteJump()
    {
        if (_target == null) return;

        _isJumpDone = true;

        float distanceToTarget = Vector2.Distance(_target.transform.position, _player.transform.position);

        if (distanceToTarget < 1) _additionalDamageInPercentage = 0.1f;
        else _additionalDamageInPercentage = 0.2f + Mathf.Floor((distanceToTarget - 1f)) * 0.2f;

        Vector3 direction = (_target.transform.position - transform.position).normalized;

        //if (_player != null && _player.TryGetComponent<BasePsionicEnergy>(out var psiEnergy)) psiEnergy.CoolDownPsionicEnegry();

        _isCheliceraStrikeCast = true;
        clawStrike.DurationChanceApplyBleedingWithJump();
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
        if (!TargetInfoQueue.TryPeek(out TargetInfo info) || info.Targets == null || info.Targets.Count == 0) return false;
        var target = info.Targets[0] as Character;
        if (target == null) return false;

        return Vector3.Distance(target.transform.position, transform.position) <= Radius && NoObstacles(target.transform.position, transform.position, _obstacle);
    }

    private void ResetBool()
    {
        _isJumpDone = false;
    }

    public void JumpWithCheliceraCast()
    {
        AnimStartCastCoroutine();
    }

    public void JumpWithCheliceraEnd()
    {
        Invoke(nameof(ResetBool), 1f);
        HandleJumpWithCheliceraEnd();
        ClearData();
        AnimCastEnded();
    }

    public void ApplyRootTrue()
    {
        IncreaseSetCooldown(CooldownTime);
        JumpWithCheliceraAnimationMove();
        _animator.applyRootMotion = true;
    }

    public void JumpEndSpeedAnim()
    {
        float timeDelay = distanceJump / 10;
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

    public void HandleJumpWithCheliceraEnd()
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
        playerMove.TargetRpcDoMove(jumpPosition, distanceJump / 10);

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

            if (target != null && !target.IsDead && _cheliceraeStrike != null && _cheliceraeStrike.IsCooldowned && !_cheliceraeStrike.Disactive) RpcCheliceraeStrike(target, additionalDamage);
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
    private void RpcCheliceraeStrike(Character target, float additionalDamage)
    {
        if (isJumpWithCheliceraChanceDamageCrit) _cheliceraeStrike.ChanceCritDamageEvolutionFour = 0.3f;
        else _cheliceraeStrike.ChanceCritDamageEvolutionFour = 0.15f;
        _cheliceraeStrike.SetAdditionalDamage(additionalDamage);
        _cheliceraeStrike.SetTarget(target);
        _cheliceraeStrike.CheliceraStrikeCast();
        _cheliceraeStrike.ClearDataCheliceraStrike();
    }
}