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

    private static readonly int RightKickTrigger = Animator.StringToHash("RightKick");
    private static readonly int LeftKickTrigger = Animator.StringToHash("LeftKick");

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
            _animator.SetTrigger(RightKickTrigger);
        else
            _animator.SetTrigger(LeftKickTrigger);

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

        CmdApplyDamage(_target.gameObject, damage);
    }

    [Command]
    private void CmdApplyDamage(GameObject targetObject, Damage damage)
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
        Hero.DamageTracker.AddDamage(damage, targetObject, isServerRequest: true);

        RpcSelfNotifyHitResult(isHit, targetObject);
    }

    [TargetRpc]
    private void RpcSelfNotifyHitResult(bool isHit, GameObject targetObject)
    {
        if (targetObject == null)
        {
            Debug.LogError("[NewPunch_Scorpion] RpcSelfNotifyHitResult: TargetObject is null!");
            return;
        }

        if (isHit)
        {
            AttackPassed(targetObject.transform);
        }
        else
        {
            AttackMissed();
        }
    }

    private void AttackPassed(Transform target)
    {
        Debug.Log("[NewPunch_Scorpion] Attack Passed");
        _comboCounter?.AddAbility(target, ScorpionAbility.Punch);
    }

    private void AttackMissed()
    {
        Debug.Log("[NewPunch_Scorpion] Attack Missed");
        _comboCounter?.ResetCounter();
    }
}
