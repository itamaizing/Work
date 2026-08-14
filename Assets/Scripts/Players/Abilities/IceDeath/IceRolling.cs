using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.AI;

public class IceRolling : Skill, IComboSeriesParticipatingSkill
{
    [Header("IceRolling Settings")] [SerializeField]
    private float _baseRange = 2f;

    [SerializeField] private float _maxRange = 4f;
    [SerializeField] private float _durationOfJumpPerCell = 0.3f;
    [SerializeField] private LayerMask _groundLayerMask;
    [SerializeField] private AudioClip _audioClip;

    [Header("Slide Physics")] [SerializeField]
    private float _castRadius = 0.45f;

    [SerializeField] private float _allyPushDistance = 1.5f;

    [SerializeField] private float _allyPushDuration = 0.25f;

    [SerializeField] private float _captureForwardOffset = 1.0f;

    private static readonly int IceRollingStartHash = Animator.StringToHash("IceRollingStart");
    private static readonly int IceRollingEndHash = Animator.StringToHash("IceRollingEnd");

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => IceRollingStartHash;

    public void IceRollingCast() => AnimStartCastCoroutine();
    public void IceRollingEnd() => AnimCastEnded();

    private Animator _animator;
    private AudioSource _audioSource;
    private Energy _energy;

    private Vector3 _mousePos = Vector3.positiveInfinity;
    private float _additionalCost = 0f;
    private float _currentRollRange = 0f;

    private const float EnergyChunkValue = 5f;
    private const float DynamicRendererJobTime = 0.2f;
    private const float TargetSearchRadius = 0.5f;
    private const float RayCastDistance = 1000f;

    private readonly HashSet<Character> _processedTargets = new();
    private readonly List<Character> _capturedTargets = new();

    private Coroutine _slideCoroutine;

    protected override bool IsCanCast
    {
        get
        {
            if (Targeting.GetTarget()?.Character != null)
                return Vector3.Distance(Targeting.GetTarget().Character.transform.position,
                    transform.position) <= AreaInfo.Radius;
            return true;
        }
    }

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);
        _animator = GetComponent<Animator>();
        _audioSource = GetComponent<AudioSource>();
    }

    protected override bool CheckResourcesOnSkill()
    {
        bool result = base.CheckResourcesOnSkill();
        Cooldown.ForceEnd();
        return result;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        Vector3 candidatePoint = Vector3.positiveInfinity;

        while (float.IsPositiveInfinity(candidatePoint.x))
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget(Targeting.GetMousePoint(), TargetSearchRadius);
                var tempTarget = Targeting.GetTempTarget()?.Targetable;

                if (tempTarget != null && tempTarget is IDamageable dmg)
                {
                    if (IsAllyTarget(dmg) || dmg as Character == Hero)
                        Targeting.ClearTempTarget();
                    else
                        candidatePoint = tempTarget.Transform.position;
                }
                else
                {
                    candidatePoint = GetMousePoint(_groundLayerMask);
                }
            }

            yield return null;
        }

        TargetInfo info = new TargetInfo();
        info.Points.Add(candidatePoint);
        callbackDataSaved(info);
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo != null && targetInfo.Points.Count > 0)
            _mousePos = targetInfo.Points[0];
    }

    protected override IEnumerator CastJob()
    {
        if (!_hero.isOwned) yield break;
        StartSlide();
        yield return null;
    }

    protected override void ClearData()
    {
        base.ClearData();
        Targeting.ClearTarget();
        Targeting.ClearTempTarget();
        _hero.Move.StopLookAt();
        _mousePos = Vector3.positiveInfinity;
        _currentRollRange = 0f;
        _additionalCost = 0f;
    }

    public override IEnumerator CustomDrawJob(float time = DynamicRendererJobTime)
    {
        while (IsPreparing)
        {
            _skillRender.SetSizeBox(1, GetFinalJumpRange());
            yield return new WaitForSeconds(time);
        }
    }

    private float GetJumpRange()
    {
        if (_energy == null)
            _energy = (Energy)_hero.Resources[ResourceType.Energy];

        float range = _baseRange;
        float costStep = EnergyChunkValue;
        for (int i = 0; i < 2; i++)
        {
            if (_energy.CurrentValue >= costStep)
            {
                range += 1f;
                costStep += EnergyChunkValue;
            }
        }

        return range;
    }

    private float GetFinalJumpRange()
    {
        float range = GetJumpRange();
        if (_isSeriesCompletedThisCast || _isSeriesPotentialFinal)
            range *= _seriesRangeMultiplier;
        return range;
    }

    private void StartSlide()
    {
        OnSeriesDamaged?.Invoke(gameObject, this);

        Vector3 startPos = _hero.transform.position;
        Vector3 lookDir = (_mousePos - startPos);
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude < 0.001f) lookDir = _hero.transform.forward;
        else lookDir.Normalize();

        bool seriesMode = _isSeriesCompletedThisCast;
        float maxRange = _maxRange * (seriesMode ? _seriesRangeMultiplier : 1f);
        float distToClick = Vector3.Distance(startPos, _mousePos);
        float finalRange;
        int extraCells;

        if (distToClick <= _baseRange)
        {
            finalRange = _baseRange;
            extraCells = 0;
        }
        else if (distToClick < maxRange)
        {
            finalRange = distToClick;
            extraCells = Mathf.CeilToInt(finalRange) - (int)_baseRange;
        }
        else
        {
            finalRange = maxRange;
            extraCells = 2;
        }

        _currentRollRange = finalRange;
        _additionalCost = extraCells * EnergyChunkValue;

        _energy?.CmdUse(_additionalCost);
        _hero.Move.SetCanMove(false);
        _hero.Move.LookAtPosition(startPos + lookDir);

        CmdNotifySlideStarted();

        _processedTargets.Clear();
        _capturedTargets.Clear();

        if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);
        _slideCoroutine = StartCoroutine(ClientSlideCoroutine(lookDir, finalRange, seriesMode));

        _isSeriesCompletedThisCast = false;
        _isSeriesPotentialFinal = false;
        _additionalCost = 0f;
        _currentRollRange = 0f;
    }

    private IEnumerator ClientSlideCoroutine(Vector3 direction, float totalRange, bool seriesMode)
    {
        float speed = 1f / _durationOfJumpPerCell;
        float duration = totalRange / speed;
        float baseRange = totalRange / (seriesMode ? _seriesRangeMultiplier : 1f);
        float elapsed = 0f;
        float stopAt = duration;

        Rigidbody rb = _hero.Rigidbody;

        while (elapsed < stopAt)
        {
            elapsed += Time.deltaTime;
            float clampedElapsed = Mathf.Min(elapsed, stopAt);

            if (rb != null) 
                rb.linearVelocity = new Vector3(direction.x * speed, rb.linearVelocity.y, direction.z * speed);

            for (int i = 0; i < _capturedTargets.Count; i++)
            {
                Character cap = _capturedTargets[i];
                if (cap == null) continue;

                float sideSign = (i == 0) ? 0f : ((i % 2 == 1) ? 1f : -1f);
                float sideOffset = sideSign * (0.5f * ((i + 1) / 2));

                Vector3 perpDir = Vector3.Cross(direction, Vector3.up).normalized;
                Vector3 desiredPos = _hero.transform.position
                                     + direction * _captureForwardOffset
                                     + perpDir * sideOffset;
                desiredPos.y = cap.transform.position.y;

                cap.transform.position = desiredPos;
                CmdSyncCapturedPosition(cap, desiredPos);
            }

            Vector3 origin = _hero.transform.position + Vector3.up * 0.5f;
            RaycastHit[] hits = Physics.SphereCastAll(
                origin, _castRadius, direction,
                speed * Time.deltaTime + 0.1f,
                _obstacle | Targeting.Layer);

            bool shouldStop = false;
            Vector3 perpDirBase = Vector3.Cross(direction, Vector3.up).normalized;

            foreach (var hit in hits)
            {
                Transform root = hit.collider.transform.root;
                if (root == _hero.transform.root) continue;

                Character ch = root.GetComponent<Character>();

                if (ch == null)
                {
                    shouldStop = true;
                    break;
                }
                
                if (_capturedTargets.Contains(ch)) continue;
                if (_processedTargets.Contains(ch)) continue;
                _processedTargets.Add(ch);

                bool isAlly = IsAllyCharacter(ch);

                if (!seriesMode)
                {
                    shouldStop = true;
                    break;
                }

                if (isAlly)
                {
                    StartCoroutine(PushAsideCoroutine(ch, perpDirBase));
                }
                else
                {
                    if (_capturedTargets.Count == 0)
                    {
                        float distTraveled = speed * elapsed;
                        float remaining = Mathf.Max(0f, baseRange - distTraveled);
                        stopAt = elapsed + remaining / speed;
                    }

                    _capturedTargets.Add(ch);
                    CmdFreezeTarget(ch, true);
                }
            }

            if (shouldStop) break;

            yield return null;
        }
        if (rb != null) rb.linearVelocity = Vector3.zero;
        _hero.Move.SetCanMove(true);

        foreach (var cap in _capturedTargets)
            if (cap != null)
                CmdFreezeTarget(cap, false);

        _capturedTargets.Clear();
        _processedTargets.Clear();

        CmdNotifySlideEnded();
        _slideCoroutine = null;
    }

    [Command]
    private void CmdNotifySlideStarted()
    {
        RpcPlayShotSound();
    }

    [Command]
    private void CmdNotifySlideEnded()
    {
        RpcOnJumpEnd();
    }
    
    [Command]
    private void CmdSyncCapturedPosition(Character target, Vector3 position)
    {
        if (target == null) return;
        target.transform.position = position;
    }

    [Command]
    private void CmdFreezeTarget(Character target, bool freeze)
    {
        if (target == null) return;
        if (target.connectionToClient != null)
            TargetRpcFreezeCharacter(target.connectionToClient, target, freeze);
        else
            RpcFreezeCharacter(target, freeze);
    }

    [TargetRpc]
    private void TargetRpcFreezeCharacter(NetworkConnection conn, Character target, bool freeze) => DoFreeze(target, freeze);

    [ClientRpc]
    private void RpcFreezeCharacter(Character target, bool freeze)
        => DoFreeze(target, freeze);

    private static void DoFreeze(Character target, bool freeze)
    {
        if (target == null) return;
        if (target.TryGetComponent(out MoveComponent move)) move.SetCanMove(!freeze);
        if (target.TryGetComponent(out NavMeshAgent agent)) agent.enabled = !freeze;
        if (target.TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = freeze;
            if (freeze) rb.linearVelocity = Vector3.zero;
        }
    }

    [ClientRpc]
    private void RpcOnJumpEnd()
    {
        if (_animator == null) return;
        _animator.ResetTrigger(IceRollingStartHash);
        _animator.SetTrigger(IceRollingEndHash);
    }

    [ClientRpc]
    private void RpcPlayShotSound() => _audioSource?.PlayOneShot(_audioClip);

    private IEnumerator PushAsideCoroutine(Character target, Vector3 pushDir)
    {
        if (target == null) yield break;
        Vector3 startPos = target.transform.position;
        Vector3 endPos = startPos + pushDir * _allyPushDistance;
        endPos.y = startPos.y;

        float timer = 0f;
        while (timer < _allyPushDuration)
        {
            if (target == null) yield break;
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / _allyPushDuration);
            target.transform.position = Vector3.Lerp(startPos, endPos, t);
            CmdSyncCapturedPosition(target, target.transform.position);
            yield return null;
        }

        target.transform.position = endPos;
    }

    private bool IsAllyTarget(IDamageable target) =>
        target.gameObject.layer == LayerMask.NameToLayer("Allies");

    private bool IsAllyCharacter(Character ch) =>
        ch.gameObject.layer == LayerMask.NameToLayer("Allies");

    private Vector3 GetMousePoint(LayerMask mask)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, RayCastDistance, mask)) return hit.point;
        return Vector3.positiveInfinity;
    }

    #region Talents

    private bool _isDamageAddFrosting = false;
    private bool _isAttackWithFrosenAddEvade = false;
    private float _frozenDuration = 0f;

    public void AttackWithFrosenAddEvade(bool value) => _isAttackWithFrosenAddEvade = value;

    public void DamageAddFrosting(bool value)
    {
        if (value == _isDamageAddFrosting) return;
        _isDamageAddFrosting = value;
        _frozenDuration = 0f;

        foreach (var ability in _hero.Abilities.Abilities)
        {
            if (ability == this) continue;
            bool isPhysical = ability.Info.AbilityForm is AbilityForm.Physical or AbilityForm.Both;
            if (!isPhysical) continue;
            if (_isDamageAddFrosting) ability.OnDamageApplied += AddFrostingToPhysical;
            else ability.OnDamageApplied -= AddFrostingToPhysical;
        }
    }

    private void AddFrostingToPhysical(GameObject target, Skill skill)
    {
        if (target == null) return;
        CmdAddFrostingToPhysical(target, _frozenDuration);
        _frozenDuration = 0f;
    }

    [Command]
    private void CmdAddFrostingToPhysical(GameObject target, float duration)
    {
        if (duration <= 0 || target == null) return;
        target.GetComponent<Character>()?.CharacterState
            .AddState(States.Frosting, duration, 0f, Schools.Water, _hero.gameObject, "IceRolling");
    }

    private void OnEnable()
    {
        if (Hero != null && Hero.Health != null)
            Hero.Health.OnBeforeDamage += HandleFrozenEvade;
    }

    private void OnDisable()
    {
        if (Hero != null && Hero.Health != null)
            Hero.Health.OnBeforeDamage -= HandleFrozenEvade;
    }

    private void HandleFrozenEvade(ref Damage damage, Skill skill)
    {
        if (!_isAttackWithFrosenAddEvade || skill?.Hero == null) return;
        var frozen = skill.Hero.CharacterState.GetState(States.Frozen) as FrozenState;
        if (frozen == null) return;
        float evadeChance = frozen.CurrentAttackSlowPercent * 40f;
        if (UnityEngine.Random.Range(0f, 100f) <= evadeChance)
        {
            damage.Value = 0f;
            Hero.Health.InvokeEvade();
        }
    }

    #endregion
    
    #region IComboSeriesParticipatingSkill

    private bool _shouldCaptureTarget = false;
    private bool _isSeriesPotentialFinal = false;
    private bool _isSeriesCompletedThisCast = false;
    private float _seriesRangeMultiplier = 1.5f;

    public event IComboSeriesParticipatingSkill.OnBeforeApplyDamageDelegate OnBeforeApplySeriesDamage;
    public event Action<GameObject, Skill> OnSeriesDamaged;

    public float EnergyCostOnHit => _additionalCost + Cost.BaseCost;
    public float RuneCostOnHit => 0f;
    public bool IsTicking { get; }

    public void OnSeriesHit(int hitCountInCurrentSeries, Character target)
    {
    }

    public void OnSeriesCompleted(Character target, int totalHits, float totalEnergySpent)
        => _isSeriesCompletedThisCast = true;

    public void OnSeriesBroken(Character target)
    {
        _isSeriesCompletedThisCast = false;
        _isSeriesPotentialFinal = false;
    }

    public void OnSeriesPotentialFinal(Skill skill, bool isPotentialFinal)
        => _isSeriesPotentialFinal = isPotentialFinal;

    #endregion
}