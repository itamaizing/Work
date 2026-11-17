using Mirror;
using System.Collections;
using UnityEngine;

public class JumpWithChelicera : Skill
{
    [SerializeField] private Character _player;
    [SerializeField] private CheliceraStrike _cheliceraeStrike;
    [SerializeField] private ClawStrike _clawStrike;
    [SerializeField] private CooldownEnergy _cooldownEnergy;
    [SerializeField] private float _basePsi = 1f;
    [SerializeField] private float _cooldownJump = 12f;

    #region Constants
    private const float BaseDamagePercent = 0.1f;
    private const float DamageStepDistance = 0.1f;
    private const float DamageStepPercent = 0.02f;
    private const float DamageRoundMultiplier = 1000f;

    private const float JumpEndTransitionDuration = 0.15f;

    private const float StopDistanceExtra = 0.5f;
    private const float PsiStepDistance = 1f;

    private const float JumpSpeedDivider = 10f;
    private const float JumpPositionSyncThreshold = 1f;

    private const float JumpCritChanceEnabled = 0.3f;
    private const float JumpCritChanceDisabled = 0.15f;

    private const float TargetSearchRadius = 0.5f;
    #endregion

    private Animator _animator;

    private static readonly int jumpEnd = Animator.StringToHash("JumpEnd");

    private float _minDistance = 0.6f;
    private bool _isCheliceraStrikeCast = false;
    private Coroutine _trackMovementDuringJumpCoroutine;

    public override bool IsPayCostStartCooldown => false;
    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerPrepare => 0;
    public bool IsCheliceraStrikeCast { get => _isCheliceraStrikeCast; set => _isCheliceraStrikeCast = value; }
    public float CooldownJump { get => _cooldownJump; set => _cooldownJump = value; }

    protected override bool IsCanCast => CheckCanCast() && _cooldownEnergy.CurrentValue >= _cooldownJump;

    private bool _jumpSequenceFinished;
    private uint _jumpId;

    private bool isJumpWithCheliceraChanceDamageCrit = false;
    public void JumpWithCheliceraChanceDamageCrit(bool value) => isJumpWithCheliceraChanceDamageCrit = value;

    private void Start() => _animator = GetComponent<Animator>();

    private void OnDisable()
    {
        Canceled -= HandleJumpWithCheliceraEnd;
        PreparingStarted -= CanMoveJumpWithCheilcera;
    }

    private void OnEnable()
    {
        PreparingStarted += CanMoveJumpWithCheilcera;
        Canceled += HandleJumpWithCheliceraEnd;
    }

    private void CanMoveJumpWithCheilcera(float castDelay)
    {
        _hero.Move.StopMoveAndAnimationMove();
        _hero.Move.SetCanMove(false);
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
        Targeting.ClearTempTarget();
        _jumpSequenceFinished = true;
        HandleJumpWithCheliceraEnd();
    }

    public override Damage GetDamage(DamageTable table = null, bool shouldModify = true)
    {
        return _cheliceraeStrike.GetDamage();
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0)
        {
            Targeting.SetTarget(targetInfo.GetTargets()[0]);
        }
        if (Targeting.GetTarget()?.Character is Character character && character.SelectedCircle != null) character.SelectedCircle.IsActive = false;
    }

    protected override void PlayPrepareAnim()
    {
        Animation.PlayPreparing(0, castSpeed: 1);
    }

    protected override IEnumerator CastJob()
    {
        var target = Targeting.GetTarget();
        if (target?.Targetable is not NetworkBehaviour networkTarget)
        {
            TryCancel(true);
            yield break;
        }

        _jumpSequenceFinished = false;
        uint jumpId = ++_jumpId;
        float distanceToTarget = Vector3.Distance(target.Transform.position, _player.transform.position);
        float damageBonus = Mathf.Round((BaseDamagePercent + Mathf.Floor(distanceToTarget / DamageStepDistance) * DamageStepPercent) * DamageRoundMultiplier) / DamageRoundMultiplier;

        _isCheliceraStrikeCast = true;
        _clawStrike.DurationChanceApplyBleedingWithJump();
        ComboContext.Bleeding.Set(typeof(JumpWithChelicera));
        ComboContext.ClawStrikeContext.Set(typeof(JumpWithChelicera));

        CmdExecuteJump(networkTarget.netId, damageBonus, jumpId);
        while (!_jumpSequenceFinished)
            yield return null;
    }

    public void HandleJumpWithCheliceraEnd()
    {
        if (_animator != null) _animator.applyRootMotion = false;
        _player.Move.StopLookAt();
        Hero.Move.SetCanMove(true);
        AnimCastEnded();
    }

    [Command]
    private void CmdExecuteJump(uint targetNetId, float damageBonus, uint jumpId)
    {
        StopCoro(ref _trackMovementDuringJumpCoroutine);
        if (!NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity targetIdentity))
        {
            RpcCompleteJump(jumpId, 0, damageBonus);
            return;
        }

        var playerMove = _player.Move;
        float duration = AreaInfo.Radius / JumpSpeedDivider;
        Vector3 jumpPosition = Vector3.MoveTowards(targetIdentity.transform.position, playerMove.transform.position, _minDistance);
        playerMove.TargetRpcDoMove(jumpPosition, duration);
        _trackMovementDuringJumpCoroutine = StartCoroutine(TrackMovementDuringJump(playerMove, targetIdentity, damageBonus, jumpId, duration));
    }

    private IEnumerator TrackMovementDuringJump(MoveComponent playerMove, NetworkIdentity target, float damageBonus, uint jumpId, float duration)
    {
        Vector3 lastPlayerPos = playerMove.transform.position;
        float stopDistance = _minDistance + StopDistanceExtra;
        double deadline = NetworkTime.time + duration + JumpPositionSyncThreshold;
        while (target != null)
        {
            Vector3 currentPlayerPos = playerMove.transform.position;
            float playerMoved = Vector3.Distance(lastPlayerPos, currentPlayerPos);
            if (playerMoved > 0.001f && _player.TryGetComponent(out BasePsionicEnergy psiEnergy))
                psiEnergy.AddPsiByDistance(playerMoved);

            if (Vector3.Distance(currentPlayerPos, target.transform.position) <= stopDistance)
            {
                RpcCompleteJump(jumpId, target.netId, damageBonus);
                yield break;
            }

            if (NetworkTime.time >= deadline) break;
            lastPlayerPos = currentPlayerPos;
            yield return null;
        }

        RpcCompleteJump(jumpId, 0, damageBonus);
    }

    [ClientRpc]
    private void RpcCompleteJump(uint jumpId, uint targetNetId, float damageBonus)
    {
        if (isOwned && (!IsExecuting || _jumpSequenceFinished || jumpId != _jumpId)) return;

        JumpEndSpeedAnim();
        if (_animator != null)
            _animator.CrossFade(jumpEnd, JumpEndTransitionDuration, 0, 0f);

        if (!isOwned) return;

        _jumpSequenceFinished = true;
        if (NetworkClient.spawned.TryGetValue(targetNetId, out NetworkIdentity identity) &&
            identity.TryGetComponent(out IDamageable target) &&
            (target is not ITargetable targetable || targetable.IsTargetable))
        {
            float criticalChance = isJumpWithCheliceraChanceDamageCrit ? JumpCritChanceEnabled : JumpCritChanceDisabled;
            _cheliceraeStrike.StrikeAfterJump(target, damageBonus, criticalChance);
        }
    }

    public void JumpWithCheliceraCast()
    {
    }
        
    public void JumpWithCheliceraEnd()
    {
        if (!isOwned) HandleJumpWithCheliceraEnd();
    }

    public void ApplyRootTrue()
    {
        Cooldown.SetIncreased(Cooldown.CooldownTime);
        _animator.applyRootMotion = true;
    }

    public void JumpEndSpeedAnim()
    {
        float timeDelay = AreaInfo.Radius / JumpSpeedDivider;
        _player.Animator.SetFloat("JumpEndSpeed", 1f / timeDelay);
    }

    private bool CheckCanCast()
    {
        var target = Targeting.GetTarget();
        return target != null && Vector3.Distance(target.Transform.position, transform.position) <= AreaInfo.Radius;
    }
}