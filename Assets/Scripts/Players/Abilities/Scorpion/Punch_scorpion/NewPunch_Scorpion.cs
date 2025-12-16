using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class NewPunch_Scorpion : Skill
{
    [Header("Ability settings")]
    [SerializeField] private Character _playerLinks;
    [SerializeField] private PassiveCombo_Scorpion _comboCounter;
    [SerializeField] private ScorpionPassive scorpionPassive;
    [SerializeField] private byte _hitsInRow = 1;

    private Coroutine _hitsInRowCoroutine;
    private Animator _animator;
    private bool _isRightKick = true;
    private WaitForSeconds _waitForMinHitsForWarmingUp;

    //private Character _target;
    private Character _lastTarget;
    private Character _currentTarget;
    private Character _runtimeTarget;

    #region Constants
    private const float MinDirectionSqrMagnitude = 0.0001f;
    private const float HitsInRowResetDelay = 2f;
    private const int MinHitsForWarmingUp = 2;
    private const float StunDuration = 1f;
    #endregion

    private static readonly int RightPunchTrigger = Animator.StringToHash("RightPunch");
    private static readonly int LeftPunchTrigger = Animator.StringToHash("LeftPunch");

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => _isRightKick ? RightPunchTrigger : LeftPunchTrigger;

    protected override bool IsCanCast => GetTarget() != null && Vector3.Distance(GetTarget().Transform.position, transform.position) <= Radius && NoObstacles(GetTarget().Transform.position, transform.position, _obstacle);
    private bool IsAllyTarget(IDamageable target) => target.gameObject.layer == TargetsLayers;

    private void Start()
    {
        _animator = GetComponent<Animator>();
        _waitForMinHitsForWarmingUp = new WaitForSeconds(MinHitsForWarmingUp);
    }

    private void OnDisable() => OnSkillCanceled -= HandleSkillCanceled;
    private void OnEnable() => OnSkillCanceled += HandleSkillCanceled;

    #region Talent
    [Header("KnockdownAddChance talent")]
    [SerializeField] private float stunningAddChance = 0.1f;
    private bool _isStunningAddChance = false;

    public void StunningAddChance(bool value) => _isStunningAddChance = value;

    [Header("WarmingUp  talent")]
    [SerializeField] private float warmingUpDuration;
    private bool _isWarningUpAddState = false;

    public void WarningUpAddState(bool value) => _isWarningUpAddState = value;
    #endregion


    private void HandleSkillCanceled()
    {
        ClearTarget();
        //_target = null;
        Hero.Move.StopLookAt();
        _hero.Move.CanMove = true;
    }

    public void NewPunch_ScorpionMoveFalse()
    {
        if (_hero == null || _hero.Move == null) return;

        var target = GetTarget() != null ? GetTarget() : _lastTarget;
        if (target == null)
        {
            _hero.Move.StopLookAt();
            return;
        }


        _hero.Move.StopMoveAndAnimationMove();
        _hero.Move.CanMove = false;

        if (target is IDamageable damageable)
        {
            Vector3 direction = damageable.transform.position - _hero.transform.position;
            bool badDirection = float.IsInfinity(damageable.transform.position.x) || direction.sqrMagnitude < MinDirectionSqrMagnitude;

            if (badDirection)
            {
                _hero.Move.StopLookAt();
                return;
            }
        }

    }

    public void NewPunch_ScorpionMoveTrue()
    {
        _hero.Move.CanMove = true;
        Hero.Move.StopLookAt();
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (GetTempTarget() == null)
        {
            if (GetMouseButton)
            {
                FindTarget();

                if (GetTempTarget() != null && GetTempTarget() is IDamageable damageable)
                {
                    if (IsAllyTarget(damageable) || damageable as Character == Hero) ClearTempTarget();

                    else
                    {
                        _hero.Move.LookAtTransform(GetTempTarget().Transform);
                        _hero.Move.LookAtTransform(GetTempTarget().Transform);
                        if (GetTempTarget() is Character character && character.SelectedCircle != null) character.SelectedCircle.IsActive = false;
                        break;
                    }
                }
            }
            yield return null;
        }

        SetTarget(GetTempTarget());

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(GetTarget());
        targetInfo.Points.Add(GetTarget().Transform.position);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (GetTarget() == null) yield return null;

        _runtimeTarget = GetTarget() as Character;

        if (_lastTarget != null && _lastTarget != _runtimeTarget)  _comboCounter.ResetCounter();

        _isRightKick = !_isRightKick;
        _lastTarget = GetTarget() as Character;

        ApplyAttackDamage();
    }

    private void ApplyAttackDamage()
    {
        if (GetTarget() == null) return;
        if (Vector2.Distance(_lastTarget.transform.position, GetTarget().Transform.position) > Radius) return;

        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(_damageValue),
            Type = DamageType,
        };

        if (GetTarget() is IDamageable damageable) CmdApplyDamage(damageable.gameObject, damage);

        ClearTarget();
    }

    [Command]
    private void CmdApplyDamage(GameObject target, Damage damage)
    {
        if (target == null)
        {
            Debug.LogError("[NewPunch_Scorpion] CmdApplyDamage: TargetObject is null!");
            return;
        }

        if (_tempTargetForDamage != target.transform)
        {
            _tempTargetForDamage = target.transform;
            _tempForDamage = target.GetComponent<IDamageable>();
        }

        if (_tempForDamage == null)
        {
            Debug.LogError("[NewPunch_Scorpion] CmdApplyDamage: Target does not have IDamageable component!");
            return;
        }

        bool isHit = _tempForDamage.TryTakeDamage(ref damage, this);
        if (isHit && _tempForDamage is Character character) AttackPassed(character);

        //RpcSelfNotifyHitResult(isHit, targetObject);
    }

    //[TargetRpc]
    //private void RpcSelfNotifyHitResult(bool isHit, Character targetObject)
    //{
    //    if (targetObject == null)
    //    {
    //        Debug.LogError("[NewPunch_Scorpion] RpcSelfNotifyHitResult: TargetObject is null!");
    //        return;
    //    }

    //    if (isHit)
    //    {
    //        AttackPassed(targetObject);
    //    }
    //    else
    //    {
    //        AttackMissed();
    //    }
    //}

    private void AttackPassed(Character target)
    {
        Debug.Log("[NewPunch_Scorpion] Attack Passed");
        _comboCounter.AddSkill(target, this);

        if (_hitsInRowCoroutine != null)
            StopCoroutine(_hitsInRowCoroutine);
        _hitsInRowCoroutine = StartCoroutine(HitsInRowTimer());

        Debug.Log($"_currentTarget: {_currentTarget}");
        Debug.Log($"_lastTarget: {_lastTarget}");

        _currentTarget = target as Character;

        if (_lastTarget != null && _lastTarget == _currentTarget) _hitsInRow++;
        else _hitsInRow = 1;

        _lastTarget = target as Character;

        if (_isWarningUpAddState && _hitsInRow >= HitsInRowResetDelay)
        {
            var state = _hero.CharacterState;
            state?.AddState(States.WarmingUpState, warmingUpDuration, 0, _hero.gameObject, name);
            _hitsInRow = 0;
        }

        if (_isStunningAddChance)
        {
            var state = target.GetComponent<CharacterState>();

            if (scorpionPassive.IsAddStateUpdateChance && state != null)
            {
                if (state.CheckForState(States.DisappointmentState)) state.AddState(States.Stun, StunDuration, 0, _hero.gameObject, name);
            }

            else
            {
                if (UnityEngine.Random.value <= stunningAddChance) state?.AddState(States.Stun, StunDuration, 0, _hero.gameObject, name);
            }
        }
    }

    public void NewPunch_ScorpionCast()
    {
        AnimStartCastCoroutine();
    }

    public void NewPunch_ScorpionEnded()
    {
        AnimCastEnded();
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0) SetTarget(targetInfo.GetTargets()[0]);
    }

    protected override void ClearData()
    {
        ClearTarget();
        _hero.Move.StopLookAt();
    }

    private IEnumerator HitsInRowTimer()
    {
        yield return _waitForMinHitsForWarmingUp;
        _hitsInRow = 0;
        _hitsInRowCoroutine = null;
    }

    //private void AttackMissed()
    //{
    //    Debug.Log("[NewPunch_Scorpion] Attack Missed");
    //    _comboCounter?.ResetCounter();
    //}
}
