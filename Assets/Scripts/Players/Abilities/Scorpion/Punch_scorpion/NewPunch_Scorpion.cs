using Mirror;
using UnityEngine;

public class NewPunch_Scorpion : AutoAttackSkill
{
    [Header("Ability settings")]
    [SerializeField] private Character _playerLinks;
    [SerializeField] private PassiveCombo_Scorpion _comboCounter;

    private Character _lastTarget = null;
    private Animator _animator;
    private bool _isRightKick = true;

    private static readonly int RightPunchTrigger = Animator.StringToHash("RightPunch");
    private static readonly int LeftPunchTrigger = Animator.StringToHash("LeftPunch");

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerAutoAttack => 0;

    private void Start()
    {
        _animator = GetComponent<Animator>();
    }

    protected override void CastAction()
    {
        if (_target == null)
        {
            Debug.LogError("[NewPunch_Scorpion] CastAction: _target is null!");
            return;
        }

        if (_lastTarget != null && _lastTarget != _target)
        {
            _comboCounter.ResetCounter();
        }

        Debug.Log($"[NewPunch_Scorpion] Starting attack on {_target.name}");

        _isRightKick = !_isRightKick;

        if (_isRightKick)
            _animator.SetTrigger(RightPunchTrigger);
        else
            _animator.SetTrigger(LeftPunchTrigger);

        _lastTarget = _target;
    }

    public void ApplyAttackDamage()
    {
        if (_target == null)
        {
            Debug.LogWarning("[NewPunch_Scorpion] ApplyAttackDamage: Target is null!");
            return;
        }

        if (Vector2.Distance(LastTargetPosition, _target.transform.position) > 2f)
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
        _target = (Character)targetInfo.Targets[0];
    }

    //private void AttackMissed()
    //{
    //    Debug.Log("[NewPunch_Scorpion] Attack Missed");
    //    _comboCounter?.ResetCounter();
    //}
}
