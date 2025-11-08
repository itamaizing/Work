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
    private IDamageable _lastTarget = null;
    private Animator _animator;
    private bool _isRightKick = true;

    private IDamageable _target;

    private static readonly int RightPunchTrigger = Animator.StringToHash("RightPunch");
    private static readonly int LeftPunchTrigger = Animator.StringToHash("LeftPunch");

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => _isRightKick ? RightPunchTrigger : LeftPunchTrigger;

    protected override bool IsCanCast => _target != null && Vector3.Distance(_target.transform.position, transform.position) <= Radius && NoObstacles(_target.transform.position, transform.position, _obstacle);

    private void Start() => _animator = GetComponent<Animator>();
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

    private bool IsTargetInRange()
    {
        return Vector3.Distance(_playerLinks.transform.position, _target.transform.position) <= Radius;
    }

    private void HandleSkillCanceled()
    {
        _target = null;
        Hero.Move.StopLookAt();
        _hero.Move.CanMove = true;
    }

    public void NewPunch_ScorpionMoveFalse()
    {
        if (_hero == null || _hero.Move == null) return;

        var target = _target != null ? _target : _lastTarget;
        if (target == null)
        {
            _hero.Move.StopLookAt();
            return;
        }


        _hero.Move.StopMoveAndAnimationMove();
        _hero.Move.CanMove = false;

        Vector3 direction = target.transform.position - _hero.transform.position;
        bool badDirection = float.IsInfinity(target.transform.position.x) || direction.sqrMagnitude < 0.0001f;

        if (badDirection)
        {
            _hero.Move.StopLookAt();
            return;
        }
    }

    public void NewPunch_ScorpionMoveTrue()
    {
        _hero.Move.CanMove = true;
        Hero.Move.StopLookAt();
    }


    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        _lastTarget = null;

        while (_target == null)
        {
            if (GetMouseButton)
            {
                _target = GetRaycastTarget();

                if (_target != null && _target is Character character) character.SelectedCircle.IsActive = true;
            }

            yield return null;
        }

        _hero.Move.LookAtTransform(_target.transform);

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Points.Add(_target.transform.position);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (_target == null) yield return null;
        if (!IsTargetInRange()) yield return null;

        if (_lastTarget != null && _lastTarget != _target)  _comboCounter.ResetCounter();

        _isRightKick = !_isRightKick;
        _lastTarget = _target;

        ApplyAttackDamage();
    }

    private void ApplyAttackDamage()
    {
        if (_target == null) return;
        if (Vector2.Distance(_lastTarget.transform.position, _target.transform.position) > Radius) return;

        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(_damageValue),
            Type = DamageType,
        };

        CmdApplyDamage(_lastTarget.gameObject, damage);

        _target = null;
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

        if (_lastTarget != null && _lastTarget == _target) _hitsInRow++;
        else _hitsInRow = 1;

        _lastTarget = target as Character;

        if (_isWarningUpAddState && _hitsInRow >= 2)
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
                if (state.CheckForState(States.DisappointmentState)) state.AddState(States.Stun, 1f, 0, _hero.gameObject, name);
            }

            else
            {
                if (UnityEngine.Random.value <= stunningAddChance) state?.AddState(States.Stun, 1f, 0, _hero.gameObject, name);
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
        if (targetInfo.Targets.Count > 0) _target = (Character)targetInfo.Targets[0];
    }

    protected override void ClearData()
    {
        _target = null;
        _hero.Move.StopLookAt();
    }

    private IEnumerator HitsInRowTimer()
    {
        yield return new WaitForSeconds(2f);
        _hitsInRow = 0;
        _hitsInRowCoroutine = null;
    }

    //private void AttackMissed()
    //{
    //    Debug.Log("[NewPunch_Scorpion] Attack Missed");
    //    _comboCounter?.ResetCounter();
    //}
}
