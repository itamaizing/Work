using System.Collections;
using UnityEngine;
using Mirror;
using System;

public class Kick_Scorpion : Skill
{
    [Header("Ability settings")]
    [SerializeField] private Character _playerLinks;
    [SerializeField] private PassiveCombo_Scorpion _comboCounter;
    [SerializeField] private ScorpionPassive _scorpionPassive;
    [SerializeField] [Range(0, 100)] private float _minDamage = 10f;
    [SerializeField] [Range(0, 100)] private float _maxDamage = 15f;

    [Header("Talent Flags")]
    private bool _isKick_ScorpionRowTalent;
    private bool _isKick_ScorpionComboTalent;
    private bool _isKick_ScorpionRowBonusTalent;

    [Header("Internal State")]
    [SerializeField] [Range(0f, 1f)] private float _baseDebuffChance = 0.3f;
    [SerializeField] [ReadOnly] private byte _hitsInRow = 1;

    private Coroutine _hitsInRowCoroutine;
    private Character _lastTarget = null;
    private Animator _animator;
    private bool _wasDamageApplied = false;
    private WaitForSeconds _waitForHitsInRowTimer;

    #region Ñonst
    private const float HitsInRowResetDelay = 2f;
    private const float MinDirectionSqrMagnitude = 0.0001f;
    private const float KnockdownDurationDefault = 13f;
    private const float KnockdownDurationCombo = 6f;
    private const float MaxHitsInRow = 4f;
    private const float SearchTargetInRadius = 1f;
    #endregion

    private static readonly int KickTrigger = Animator.StringToHash("KickAA");

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => KickTrigger;

    protected override bool IsCanCast => GetTarget() != null && Vector3.Distance(GetTarget().Transform.position, transform.position) <= Radius && NoObstacles(GetTarget().Transform.position, transform.position, _obstacle);
    private bool IsAllyTarget(IDamageable target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");

    public float DamageRange => UnityEngine.Random.Range(_minDamage, _maxDamage);

    private void Start()
    {
        _animator = GetComponent<Animator>();
        _waitForHitsInRowTimer = new WaitForSeconds(HitsInRowResetDelay);
    }

    private void OnDisable() => OnSkillCanceled -= HandleSkillCanceled;
    private void OnEnable() => OnSkillCanceled += HandleSkillCanceled;

    private void HandleSkillCanceled()
    {
        _wasDamageApplied = false;
        ClearTarget();
        ClearTempTarget();
        //_target = null;
        Hero.Move.StopLookAt();
        _hero.Move.CanMove = true;
        AnimCastEnded();
        if (_hitsInRowCoroutine != null) StopCoroutine(_hitsInRowCoroutine);
    }

    public void Kick_ScorpionMoveFalse()
    {
        if (_hero == null || _hero.Move == null) return;

        var target = GetTargetCharacter() != null ? GetTargetCharacter() : _lastTarget;
        if (target == null)
        {
            _hero.Move.StopLookAt();
            return;
        }


        _hero.Move.StopMoveAndAnimationMove();
        _hero.Move.CanMove = false;

        Vector3 direction = target.transform.position - _hero.transform.position;
        bool badDirection = float.IsInfinity(target.transform.position.x) || direction.sqrMagnitude < MinDirectionSqrMagnitude;

        if (badDirection)
        {
            _hero.Move.StopLookAt();
            return;
        }

        _hero.Move.LookAtPosition(GetTargetCharacter().transform.position);
    }

    public void Kick_ScorpionMoveTrue()
    {
        _hero.Move.CanMove = true;
        Hero.Move.StopLookAt();
    }

    private bool IsTargetInRange()
    {
        return Vector3.Distance(_playerLinks.transform.position, GetTargetCharacter().transform.position) <= Radius;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        _wasDamageApplied = false;

        while (GetTempTarget() == null)
        {
            if (GetMouseButton)
            {
                FindTarget(SearchTargetInRadius, GetMousePoint());

                if (GetTempTarget() != null && GetTempTarget() is IDamageable damageable)
                {
                    if (IsAllyTarget(damageable) || damageable as Character == Hero) ClearTempTarget();

                    else
                    {
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
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (GetTarget() == null) yield return null;
        if (!IsTargetInRange()) yield return null;

        if (_lastTarget != null && _lastTarget != GetTarget() as Character) _comboCounter.ResetCounter();

        if (_hitsInRowCoroutine != null) StopCoroutine(_hitsInRowCoroutine);

        _lastTarget = GetTarget() as Character;

        ApplyAttackDamageKick();
    }

    private void ApplyAttackDamageKick()
    {
        if (_wasDamageApplied) return;
        if (GetTarget() == null) return;
        if (Vector2.Distance(_lastTarget.transform.position, GetTarget().Transform.position) > Radius) return;

        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(DamageRange),
            Type = DamageType,
        };

        _wasDamageApplied = true;

        if (GetTarget() is IDamageable damageable) CmdApplyDamage(damageable.gameObject, damage);
    }

    private IEnumerator HitsInRowTimer()
    {
        yield return _waitForHitsInRowTimer;
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

        if (_isKick_ScorpionRowTalent)
        {
            if (_scorpionPassive.IsAddStateUpdateChance)
            {
                if (state.CheckForState(States.DisappointmentState)) state?.AddState(States.Knockdown, KnockdownDurationDefault, 0, _hero.gameObject, name);
            }

            else
            {
                if (_isKick_ScorpionRowBonusTalent)
                {
                    chance = _baseDebuffChance * Mathf.Pow(2, _hitsInRow - 1);

                    if (UnityEngine.Random.value <= Mathf.Clamp01(chance))
                    {
                        state?.AddState(States.Knockdown, KnockdownDurationDefault, 0, _hero.gameObject, name);
                        _hitsInRow = 1;
                    }

                    else _hitsInRow = (byte)Mathf.Min(_hitsInRow + 1, MaxHitsInRow);
                }

                else
                {
                    chance = _baseDebuffChance;
                    if (UnityEngine.Random.value <= Mathf.Clamp01(chance)) state?.AddState(States.Knockdown, KnockdownDurationDefault, 0, _hero.gameObject, name);
                }
            }
        }

        else _hitsInRow = 1;

        if (_isKick_ScorpionComboTalent && state != null)
        {
            int comboStacks = state.CheckStateStacks(States.ComboState);
            for (int i = 0; i < comboStacks; i++)
            {
                state.AddState(States.Knockdown, KnockdownDurationCombo, 0, _hero.gameObject, name);
            }
        }
    }

    [Command]
    private void CmdApplyDamage(GameObject target, Damage damage)
    {
        if (target == null) return;

        IDamageable targetHealth = target.GetComponent<IDamageable>();
        if (targetHealth == null) return;

        bool isHit = targetHealth.TryTakeDamage(ref damage, this);
        Hero.DamageTracker.AddDamage(damage, target, isServerRequest: true);

        if (isHit && targetHealth is Character character) AttackPassed(character);
    }

    public void Kick_ScorpionRowTalent(bool value)
    {
        _isKick_ScorpionRowTalent = value;
    }

    public void Kick_ScorpionRowBonusTalent(bool value)
    {
        _isKick_ScorpionRowBonusTalent = value;
    }

    public void Kick_ScorpionComboTalent(bool value)
    {
        _isKick_ScorpionComboTalent = value;
    }

    public void Kick_ScorpionCast()
    {
        AnimStartCastCoroutine();
    }

    public void Kick_ScorpionEnded()
    {
        AnimCastEnded();
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0) SetTarget(targetInfo.GetTargets()[0]);
    }

    protected override void ClearData()
    {
        _wasDamageApplied = false;
        ClearTarget();
        ClearTempTarget();
        _hero.Move.StopLookAt();
        AnimCastEnded();
        if (_hitsInRowCoroutine != null) StopCoroutine(_hitsInRowCoroutine);
    }
}