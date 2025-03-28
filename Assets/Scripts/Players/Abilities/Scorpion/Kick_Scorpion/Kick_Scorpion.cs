using System.Collections;
using UnityEngine;
using Mirror;

public class Kick_Scorpion : AutoAttackSkill
{
    [Header("Ability settings")]
    [SerializeField] private Character _playerLinks;
    [SerializeField] private PassiveCombo_Scorpion _comboCounter;
    [SerializeField] [Range(0, 100)] private float _minDamage = 10f;
    [SerializeField] [Range(0, 100)] private float _maxDamage = 15f;

    [Header("Talent Flags")]
    private bool isKick_ScorpionRowTalent;
    private bool isKick_ScorpionComboTalent;

    [Header("Internal State")]
    [SerializeField] [Range(0f, 1f)] private float _baseDebuffChance = 0.2f;
    [SerializeField] [ReadOnly] private byte _hitsInRow = 1;

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
        if (_target == null) return;

        if (_lastTarget != null && _lastTarget != _target)
            _comboCounter?.ResetCounter();

        if (_hitsInRowCoroutine != null)
            StopCoroutine(_hitsInRowCoroutine);

        _animator.SetTrigger(KickTrigger);
        _lastTarget = _target;
    }

    public void ApplyAttackDamageKick()
    {
        if (_target == null) return;

        if (Vector2.Distance(LastTargetPosition, _target.transform.position) > Radius)
            return;

        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(DamageRange),
            Type = DamageType,
        };

        CmdApplyDamage(_target, damage);
    }

    private IEnumerator HitsInRowTimer()
    {
        yield return new WaitForSeconds(2f);
        _hitsInRow = 1;
        _hitsInRowCoroutine = null;
    }

    private void AttackPassed(Character target)
    {
        _comboCounter.AddSkill(target, this);

        if (_hitsInRowCoroutine != null)
            StopCoroutine(_hitsInRowCoroutine);

        _hitsInRowCoroutine = StartCoroutine(HitsInRowTimer());

        var state = target.GetComponent<CharacterState>();
        float chance = 0f;

        if (isKick_ScorpionRowTalent)
        {
            chance = _baseDebuffChance * Mathf.Pow(2, _hitsInRow - 1);

            if (Random.value <= Mathf.Clamp01(chance))
            {
                state?.AddState(States.Knockdown, 6f, 0, _hero.gameObject, name);
                _hitsInRow = 1;
            }
            else _hitsInRow = (byte)Mathf.Min(_hitsInRow + 1, 4);
        }
        else _hitsInRow = 1;

        if (isKick_ScorpionComboTalent && state != null)
        {
            int comboStacks = state.CheckStateStacks(States.ComboState);
            for (int i = 0; i < comboStacks; i++)
            {
                state.AddState(States.Knockdown, 6f, 0, _hero.gameObject, name);
            }
        }
    }

    [Command]
    private void CmdApplyDamage(Character targetObject, Damage damage)
    {
        if (targetObject == null) return;

        IDamageable targetHealth = targetObject.GetComponent<IDamageable>();
        if (targetHealth == null) return;

        bool isHit = targetHealth.TryTakeDamage(ref damage, this);
        Hero.DamageTracker.AddDamage(damage, targetObject.gameObject, isServerRequest: true);

        if (isHit) AttackPassed(targetObject);
    }

    public void Kick_ScorpionRowTalent(bool value)
    {
        isKick_ScorpionRowTalent = value;
    }

    public void Kick_ScorpionComboTalent(bool value)
    {
        isKick_ScorpionComboTalent = value;
    }
}
