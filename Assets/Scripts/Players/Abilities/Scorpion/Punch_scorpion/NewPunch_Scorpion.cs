using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class NewPunch_Scorpion : Skill
{
    [Header("Ability settings")]
    [SerializeField] private Character _playerLinks;
    [SerializeField] private PassiveCombo_Scorpion _comboCounter;

    private Character _lastTarget = null;
    private Animator _animator;
    private bool _isRightKick = true;

    private Character _target;

    private static readonly int RightPunchTrigger = Animator.StringToHash("RightPunch");
    private static readonly int LeftPunchTrigger = Animator.StringToHash("LeftPunch");

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    protected override bool IsCanCast => _target != null && Vector3.Distance(_target.transform.position, transform.position) <= Radius && NoObstacles(_target.transform.position, transform.position, _obstacle);

    private void Start() => _animator = GetComponent<Animator>();
    private void OnDisable() => OnSkillCanceled -= HandleSkillCanceled;
    private void OnEnable() => OnSkillCanceled += HandleSkillCanceled;

    private bool IsTargetInRange()
    {
        return Vector3.Distance(_playerLinks.transform.position, _target.transform.position) <= Radius;
    }

    private void HandleSkillCanceled()
    {
        _target = null;
        Hero.Move.StopLookAt();
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (_target == null)
        {
            if (GetMouseButton)
            {
                _target = GetRaycastTarget();

                if (_target != null)
                    _target.SelectedCircle.IsActive = true;
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

        if (_isRightKick) _animator.SetTrigger(RightPunchTrigger);
        else _animator.SetTrigger(LeftPunchTrigger);

        _lastTarget = _target;

        yield return null;
    }

    public void ApplyAttackDamage()
    {
        if (_target == null)
        {
            Debug.LogWarning("[NewPunch_Scorpion] ApplyAttackDamage: Target is null!");
            return;
        }

        if (Vector2.Distance(_lastTarget.transform.position, _target.transform.position) > 2f)
        {
            Debug.LogWarning("[NewPunch_Scorpion] Target moved too far!");
            return;
        }

        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(_damageValue),
            Type = DamageType,
        };

        CmdApplyDamage(_target, damage);

        _target = null;
    }

    [Command]
    private void CmdApplyDamage(Character targetObject, Damage damage)
    {
        if (targetObject == null)
        {
            Debug.LogError("[NewPunch_Scorpion] CmdApplyDamage: TargetObject is null!");
            return;
        }

        if (_tempTargetForDamage != targetObject.transform)
        {
            _tempTargetForDamage = targetObject.transform;
            _tempForDamage = targetObject.GetComponent<IDamageable>();
        }

        if (_tempForDamage == null)
        {
            Debug.LogError("[NewPunch_Scorpion] CmdApplyDamage: Target does not have IDamageable component!");
            return;
        }

        bool isHit = _tempForDamage.TryTakeDamage(ref damage, this);
        Hero.DamageTracker.AddDamage(damage, targetObject.gameObject, isServerRequest: true);
        AttackPassed(targetObject);

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
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Targets.Count > 0) _target = (Character)targetInfo.Targets[0];
    }

    protected override void ClearData()
    {
        _hero.Move.StopLookAt();
    }

    //private void AttackMissed()
    //{
    //    Debug.Log("[NewPunch_Scorpion] Attack Missed");
    //    _comboCounter?.ResetCounter();
    //}
}
