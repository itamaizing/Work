using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class JumpWithChelicera : Skill
{
    [SerializeField] private Character _player;
    [SerializeField] private CheliceraStrike _cheliceraeStrike;
    [SerializeField] private ClawStrike _clawStrike;
    [SerializeField] private CooldownEnergy _cooldownEnergy;
    [SerializeField] private float _basePsi = 1f;
    [SerializeField] private float _distanceJump = 4f;
    [SerializeField] private float _cooldownJump = 12f;

    #region Constants
    private const float BaseDamagePercent = 0.1f;
    private const float DamageStepDistance = 0.1f;
    private const float DamageStepPercent = 0.02f;
    private const float DamageRoundMultiplier = 1000f;

    private const float JumpEndTransitionDuration = 0.15f;

    private const float StopDistanceExtra = 0.5f;
    private const float PsiStepDistance = 0.1f;

    private const float JumpSpeedDivider = 10f;

    private const float JumpCritChanceEnabled = 0.3f;
    private const float JumpCritChanceDisabled = 0.15f;

    private const float TargetSearchRadius = 0.5f;
    #endregion

    private Animator _animator;

    private static readonly int jumpStart = Animator.StringToHash("JumpStart");
    private static readonly int jumpEnd = Animator.StringToHash("JumpEnd");

    private float _delayBeforeJump = 1f;
    private float _minDistance = 0.6f;
    private float _additionalDamageInPercentage;
    private bool _isJumpDone = false;
    private bool _isCheliceraStrikeCast = false;
    private Coroutine _trackMovementDuringJumpCoroutine;

    public override bool IsPayCostStartCooldown => false;
    protected override int AnimTriggerCast => jumpStart;
    protected override int AnimTriggerCastDelay => 0;
    public bool IsJumpDone { get => _isJumpDone; set => _isJumpDone = value; }
    public bool IsCheliceraStrikeCast { get => _isCheliceraStrikeCast; set => _isCheliceraStrikeCast = value; }
    public float CooldownJump { get => _cooldownJump; set => _cooldownJump = value; }

    protected override bool IsCanCast => CheckCanCast() && _cooldownEnergy.CurrentValue >= _cooldownJump;
    private bool IsAllyTarget(IDamageable target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");

    private bool isJumpWithCheliceraChanceDamageCrit = false;
    public void JumpWithCheliceraChanceDamageCrit(bool value) => isJumpWithCheliceraChanceDamageCrit = value;

    private void Start() => _animator = GetComponent<Animator>();
    private void OnDisable() => Canceled -= HandleJumpWithCheliceraEnd;
    private void OnEnable() => Canceled += HandleJumpWithCheliceraEnd;

    protected override void ClearData()
    {
        ClearTarget();
        ClearTempTarget();
        if (_trackMovementDuringJumpCoroutine != null) StopCoroutine(_trackMovementDuringJumpCoroutine);
        AnimCastEnded();
    }

    public void JumpWithCheliceraAnimationMove()
    {
        if (_hero == null || _hero.Move == null) return;
        _hero.Move.StopMoveAndAnimationMove();
        _hero.Move.SetCanMove(false);
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _castDeley = _delayBeforeJump;
        if (targetInfo.GetTargets().Count > 0)
        {
            SetTarget(targetInfo.GetTargets()[0]);
            _cheliceraeStrike.SetTarget(targetInfo.GetTargets()[0]);
        }
        if (GetTarget() is Character character && character.SelectedCircle != null) character.SelectedCircle.IsActive = false;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        while (GetTempTarget() == null)
        {
            if (GetMouseButton)
            {
                FindTarget(TargetSearchRadius, GetMousePoint());

                if (GetTempTarget() != null && GetTempTarget() is IDamageable damageable)
                {
                    if (IsAllyTarget(damageable) || damageable as Character == Hero) ClearTempTarget();

                    else
                    {
                        if (GetTempTarget() is Character character && character.SelectedCircle != null) character.SelectedCircle.IsActive = false;
                        break;
                    }
                }
            }
            yield return null;
        }

        SetTarget(GetTempTarget());

        targetInfo.Points.Add(GetTarget().Transform.position);
        targetInfo.AddTarget(GetTarget());
        callbackDataSaved.Invoke(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (GetTarget() == null)
        {
            TryCancel();
            yield break;
        }

        if (!CheckCanCast())
        {
            TryCancel();
            yield break;
        }

        ExecuteJump(GetTarget());
        yield return null;
    }

    private void ExecuteJump(ITargetable target)
    {
        if (target == null) return;

        _isJumpDone = true;

        float distanceToTarget = Vector3.Distance(GetTarget().Transform.position, _player.transform.position);
        _additionalDamageInPercentage = Mathf.Round((BaseDamagePercent + Mathf.Floor(distanceToTarget / DamageStepDistance) * DamageStepPercent) * DamageRoundMultiplier) / DamageRoundMultiplier;
        Vector3 direction = (GetTarget().Transform.position - transform.position).normalized;

        _isCheliceraStrikeCast = true;
        _clawStrike.DurationChanceApplyBleedingWithJump();

        if (target is Character character)
        {
            CmdExecuteJump(_player.gameObject, character.netId, direction, _additionalDamageInPercentage);
        }
        else if (target is NetworkBehaviour nb)
        {
            CmdExecuteJumpToPosition(_player.gameObject, GetTarget().Transform.position, nb.netId, _additionalDamageInPercentage);
        }
    }


    private void HandleJumpAnimEnd()
    {
        if (_animator != null) _animator.CrossFade(jumpEnd, JumpEndTransitionDuration);
    }

    public void HandleJumpWithCheliceraEnd()
    {
        _animator.applyRootMotion = false;
        _player.Move.StopLookAt();
        Hero.Move.SetCanMove(true);
        if (_trackMovementDuringJumpCoroutine != null) StopCoroutine(_trackMovementDuringJumpCoroutine);
        AnimCastEnded();
    }

    [Command]
    private void CmdExecuteJump(GameObject player, uint targetNetId, Vector3 direction, float additionalDamage)
    {
        if (!NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity identity)) return;
        Character targetCharacter = identity.GetComponent<Character>();
        if (targetCharacter == null) return;

        MoveComponent playerMove = player.GetComponent<MoveComponent>();
        Vector3 jumpPosition = Vector3.MoveTowards(targetCharacter.transform.position, player.transform.position, _minDistance);
        playerMove.TargetRpcDoMove(jumpPosition, _distanceJump / JumpSpeedDivider);
        StartCoroutine(TrackMovementDuringJumpCoroutine(playerMove, targetCharacter.netId, additionalDamage));
    }

    [Command]
    private void CmdExecuteJumpToPosition(GameObject player, Vector3 targetPosition, uint targetNetId, float additionalDamage)
    {
        MoveComponent playerMove = player.GetComponent<MoveComponent>();
        Vector3 jumpPosition = Vector3.MoveTowards(targetPosition, player.transform.position, _minDistance);
        playerMove.TargetRpcDoMove(jumpPosition, _distanceJump / 10);

        if (_trackMovementDuringJumpCoroutine != null) StopCoroutine(TrackMovementDuringJumpCoroutine(playerMove, targetNetId, additionalDamage));
        _trackMovementDuringJumpCoroutine = StartCoroutine(TrackMovementDuringJumpCoroutine(playerMove, targetNetId, additionalDamage));
    }

    private IEnumerator TrackMovementDuringJumpCoroutine(MoveComponent playerMove, uint targetNetId, float additionalDamage)
    {
        Vector3 lastPlayerPos = playerMove.transform.position;

        float playerDistanceAccumulator = 0f;
        float stopDistance = _minDistance + StopDistanceExtra;

        Transform targetTransform = null;
        IDamageable targetDamageable = null;

        if (NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity identity))
        {
            targetTransform = identity.transform;
            targetDamageable = identity.GetComponent<IDamageable>();
        }

        while (targetTransform != null && Vector3.Distance(playerMove.transform.position, targetTransform.position) > stopDistance)
        {
            Vector3 currentPlayerPos = playerMove.transform.position;
            float playerMoved = Vector3.Distance(lastPlayerPos, currentPlayerPos);
            playerDistanceAccumulator += playerMoved;

            while (playerDistanceAccumulator >= PsiStepDistance)
            {
                playerDistanceAccumulator -= PsiStepDistance;
                if (_player != null && _player.TryGetComponent(out BasePsionicEnergy psiEnergy)) psiEnergy.AddAndResetDecay(_basePsi);
            }

            lastPlayerPos = currentPlayerPos;
            yield return null;
        }

        RpcHandleJumpAnimEnd();

        if (targetDamageable is NetworkBehaviour net)
            RpcCheliceraeStrikeByNetId(net.netId, additionalDamage);
    }

    [ClientRpc]
    private void RpcHandleJumpAnimEnd()
    {
        HandleJumpAnimEnd();
    }

    [ClientRpc]
    private void RpcCheliceraeStrikeByNetId(uint netId, float additionalDamage)
    {
        if (!NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity identity)) return;

        if (identity.TryGetComponent(out IDamageable target))
        {
            _cheliceraeStrike.ChanceCritDamageEvolutionFour = isJumpWithCheliceraChanceDamageCrit ? JumpCritChanceEnabled : JumpCritChanceDisabled;
            _cheliceraeStrike.SetAdditionalDamage(additionalDamage);
            _cheliceraeStrike.CheliceraStrikeCast();
            _cheliceraeStrike.ClearDataCheliceraStrike();
        }
    }

    public void JumpWithCheliceraCast() => AnimStartCastCoroutine();
    public void JumpWithCheliceraEnd()
    {
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
        float timeDelay = _distanceJump / JumpSpeedDivider;
        _player.Animator.SetFloat("JumpEndSpeed", 1f / timeDelay);
    }

    private bool CheckCanCast()
    {
        if (GetTarget() == null) return false;
        return Vector3.Distance(GetTarget().Transform.position, transform.position) <= AreaInfo.Radius && NoObstacles(GetTarget().Transform.position, transform.position, _obstacle);
    }
}