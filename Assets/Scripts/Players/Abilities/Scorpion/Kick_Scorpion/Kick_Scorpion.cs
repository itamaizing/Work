using System.Collections;
using UnityEngine;
using Mirror;

public class Kick_Scorpion : AutoAttackSkill
{
    [Header("Ability settings")]
    [SerializeField] private Character _playerLinks;
    [SerializeField] private Sub_LavaPool_Scorpion _pool;
    [SerializeField] private PassiveCombo_Scorpion _comboCounter;
    [SerializeField] [Range(0, 100)] private float _minDamage = 10f;
    [SerializeField] [Range(0, 100)] private float _maxDamage = 15f;

    [Header("Debug info")]
    [SerializeField] [Range(0f, 1f)] private float _debuffApplyChance = 0.1f;
    [SerializeField] [ReadOnly] private byte _counterRow = 1;

    private Coroutine _hitsInRowCoroutine;
    private Character _lastTarget = null;
    private Animator _animator;

    private static readonly int KickTrigger = Animator.StringToHash("KickAA");

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerAutoAttack => 0;

    public float DamageRange => Random.Range(_minDamage, _maxDamage);

    private void Start()
    {
        _animator = GetComponent<Animator>();
    }

    protected override void CastAction()
    {
        if (_target == null)
        {
            Debug.LogError("[Kick_Scorpion] CastAction: _target is null!");
            return;
        }

        if (_lastTarget != null && _lastTarget != _target)
        {
            _comboCounter?.ResetCounter();
        }

        if (_hitsInRowCoroutine != null)
        {
            StopCoroutine(_hitsInRowCoroutine);
            _hitsInRowCoroutine = null;
        }

        Debug.Log($"[Kick_Scorpion] Preparing attack on {_target.name}");

        _animator.SetTrigger(KickTrigger);

        _lastTarget = _target;
    }

    /// <summary>
    /// Вызывается из анимации удара.
    /// </summary>
    public void ApplyAttackDamageKick()
    {
        if (_target == null)
        {
            Debug.LogWarning("[Kick_Scorpion] ApplyAttackDamageKick: Target is null!");
            return;
        }

        if (Vector2.Distance(LastTargetPosition, _target.transform.position) > 2f)
        {
            Debug.LogWarning("[Kick_Scorpion] Target moved too far!");
            return;
        }

        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(DamageRange),
            Type = DamageType,
        };

        CmdApplyDamage(_target, damage);
    }

    private void AttackPassed(Character target)
    {
        Debug.LogWarning("[Kick_Scorpion] Attack Passed!");

        _comboCounter?.AddAbility(target, ScorpionAbility.Kick);
        _counterRow *= 2;
        _hitsInRowCoroutine = StartCoroutine(HitsInRowTimer());

        if (Random.value <= Mathf.Clamp01(_debuffApplyChance * _counterRow))
        {
            target.GetComponent<CharacterState>()?.CmdAddState(States.Knockdown, 6f, 0, _hero.gameObject, name);
            _counterRow = 1;
        }
    }

    private void AttackMissed()
    {
        Debug.LogWarning("[Kick_Scorpion] Attack Missed!");
        _comboCounter?.ResetCounter();
    }

    private IEnumerator HitsInRowTimer()
    {
        yield return new WaitForSeconds(CastDeley + 1f);
        _counterRow = 1;
        _hitsInRowCoroutine = null;
    }

    [Command]
    private void CmdApplyDamage(Character targetObject, Damage damage)
    {
        if (targetObject == null)
        {
            Debug.LogError("[Kick_Scorpion] CmdApplyDamage: TargetObject is null!");
            return;
        }

        IDamageable targetHealth = targetObject.GetComponent<IDamageable>();
        if (targetHealth == null)
        {
            Debug.LogError("[Kick_Scorpion] CmdApplyDamage: Target has no IDamageable component!");
            return;
        }

        bool isHit = targetHealth.TryTakeDamage(ref damage, this);
        Hero.DamageTracker.AddDamage(damage, targetObject.gameObject, isServerRequest: true);

        RpcSelfNotifyHitResult(isHit, targetObject);
    }

    [TargetRpc]
    private void RpcSelfNotifyHitResult(bool isHit, Character target)
    {
        if (isHit)
        {
            AttackPassed(target);
        }
        else
        {
            AttackMissed();
        }
    }
}
