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

    private const float JumpCritChanceEnabled = 0.3f;
    private const float JumpCritChanceDisabled = 0.15f;

    private const float TargetSearchRadius = 0.5f;
    #endregion

    private Animator _animator;
    private ITargetable _currentTarget;

    private static readonly int jumpStart = Animator.StringToHash("JumpStart");
    private static readonly int jumpEnd = Animator.StringToHash("JumpEnd");

    private float _minDistance = 0.6f;
    private float _additionalDamageInPercentage;
    private bool _isJumpDone = false;
    private bool _isCheliceraStrikeCast = false;
    private Coroutine _trackMovementDuringJumpCoroutine;

    public override bool IsPayCostStartCooldown => false;
    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerCastDelay => 0;
    public bool IsJumpDone { get => _isJumpDone; set => _isJumpDone = value; }
    public bool IsCheliceraStrikeCast { get => _isCheliceraStrikeCast; set => _isCheliceraStrikeCast = value; }
    public float CooldownJump { get => _cooldownJump; set => _cooldownJump = value; }

    protected override bool IsCanCast => CheckCanCast() && _cooldownEnergy.CurrentValue >= _cooldownJump;

    private bool _jumpSequenceFinished;

    private bool isJumpWithCheliceraChanceDamageCrit = false;
    public void JumpWithCheliceraChanceDamageCrit(bool value) => isJumpWithCheliceraChanceDamageCrit = value;

    private void Start() => _animator = GetComponent<Animator>();

    private void OnDisable()
    {
        Canceled -= HandleJumpWithCheliceraEnd;
        CastDeleyStarted -= CanMoveJumpWithCheilcera;
    }

    private void OnEnable()
    {
        CastDeleyStarted += CanMoveJumpWithCheilcera;
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
        _currentTarget = null;
        if (_trackMovementDuringJumpCoroutine != null) StopCoroutine(_trackMovementDuringJumpCoroutine);
        AnimCastEnded();
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0)
        {
            Targeting.SetTarget(targetInfo.GetTargets()[0]);
            _cheliceraeStrike.Targeting.SetTarget(targetInfo.GetTargets()[0]);
        }
        if (Targeting.GetTarget()?.Character is Character character && character.SelectedCircle != null) character.SelectedCircle.IsActive = false;
    }

    protected override IEnumerator CastJob()
    {
        if (Targeting.GetTarget() == null)
        {
            TryCancel(true);
            yield break;
        }

        _jumpSequenceFinished = false;
        
        _hero.Animator.SetFloat(HashAnimPlayer.CastSpeed, GetCastSpeed());
        _hero.Animator.SetTrigger(jumpStart);
        _hero.NetworkAnimator.SetTrigger(jumpStart);

        ExecuteJump(Targeting.GetTarget().Targetable);

        while (!_jumpSequenceFinished)
            yield return null;
    }

    private void ExecuteJump(ITargetable target)
    {
        if (target == null) return;

        _isJumpDone = true;

        float distanceToTarget = Vector3.Distance(Targeting.GetTarget().Transform.position, _player.transform.position);
        _additionalDamageInPercentage = Mathf.Round((BaseDamagePercent + Mathf.Floor(distanceToTarget / DamageStepDistance) * DamageStepPercent) * DamageRoundMultiplier) / DamageRoundMultiplier;
        Vector3 direction = (Targeting.GetTarget().Transform.position - transform.position).normalized;

        _isCheliceraStrikeCast = true;
        _clawStrike.DurationChanceApplyBleedingWithJump();

        ComboContext.Bleeding.Set(typeof(JumpWithChelicera));
        
        ComboContext.ClawStrikeContext.Set(typeof(JumpWithChelicera));

        if (target is Character character)
        {
            CmdExecuteJump(_player.gameObject, character.netId, direction, _additionalDamageInPercentage);
        }
        else if (target is NetworkBehaviour nb)
        {
            CmdExecuteJumpToPosition(_player.gameObject, Targeting.GetTarget().Transform.position, nb.netId, _additionalDamageInPercentage);
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
        playerMove.TargetRpcDoMove(jumpPosition, AreaInfo.Radius / JumpSpeedDivider);
        StartCoroutine(TrackMovementDuringJumpCoroutine(playerMove, targetCharacter.netId, additionalDamage));
    }

    [Command]
    private void CmdExecuteJumpToPosition(GameObject player, Vector3 targetPosition, uint targetNetId, float additionalDamage)
    {
        MoveComponent playerMove = player.GetComponent<MoveComponent>();
        Vector3 jumpPosition = Vector3.MoveTowards(targetPosition, player.transform.position, _minDistance);
        playerMove.TargetRpcDoMove(jumpPosition, AreaInfo.Radius / 10);

        if (_trackMovementDuringJumpCoroutine != null) StopCoroutine(TrackMovementDuringJumpCoroutine(playerMove, targetNetId, additionalDamage));
        _trackMovementDuringJumpCoroutine = StartCoroutine(TrackMovementDuringJumpCoroutine(playerMove, targetNetId, additionalDamage));
    }

    private IEnumerator TrackMovementDuringJumpCoroutine(MoveComponent playerMove, uint targetNetId, float additionalDamage)
    {
        Vector3 lastPlayerPos = playerMove.transform.position;
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
            
            if (playerMoved > 0.001f && _player != null && _player.TryGetComponent(out BasePsionicEnergy psiEnergy))
            {
                psiEnergy.AddPsiByDistance(playerMoved);
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
            
            _cheliceraeStrike.IsTriggeredByJump = true;
            
            _cheliceraeStrike.CheliceraStrikeCast();
            _cheliceraeStrike.ClearDataCheliceraStrike();
        }
    }

    public void JumpWithCheliceraCast()
    {
    }
        
    public void JumpWithCheliceraEnd()
    {
        HandleJumpWithCheliceraEnd();
        ClearData();
        _jumpSequenceFinished = true;
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
        if (Targeting.GetTarget() != null)
        {
            float distance = Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position);
            return distance <= AreaInfo.Radius;
        }
        return false;
    }
}