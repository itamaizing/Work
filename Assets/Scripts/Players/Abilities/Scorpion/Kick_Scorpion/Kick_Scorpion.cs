using System.Collections;
using UnityEngine;
using Mirror;
using System;
using Random = UnityEngine.Random;

public class Kick_Scorpion : Skill, IComboParticipatingSkill
{
    [Header("Ability settings")]
    [SerializeField] private Character _playerLinks;
    [SerializeField] private ScorpionPassive _scorpionPassive;
    [SerializeField] [Range(0, 100)] private float _minDamage = 10f;
    [SerializeField] [Range(0, 100)] private float _maxDamage = 15f;

    [Header("Talent Flags")]
    private bool _isKick_ScorpionRowTalent;
    private bool _isKick_ScorpionComboTalent;
    private bool _isKick_ScorpionRowBonusTalent;

    [Header("Internal State")]
    private byte _kickHitsInRow = 0;
    private const float BaseKnockdownChance = 0.30f;
    private const float KnockDownPerHit = 0.20f;

    private Coroutine _hitsInRowCoroutine;
    private Character _lastTarget = null;
    private Animator _animator;
    private bool _wasDamageApplied = false;
    private WaitForSeconds _waitForHitsInRowTimer;
    
    private IDamageable _castTarget;

    public event IComboParticipatingSkill.OnBeforeApplyDamageDelegate OnBeforeApplyParticipatingDamage;
    public event Action<GameObject, Skill> OnDamaged;
    public void OnFinalComboSkill(GameObject target)
    {
        var state = target.GetComponent<CharacterState>();
        if(isServer)
            state?.AddState(States.Knockdown, KnockdownDurationDefault, 0, _hero.gameObject, name);
    }

    public void OnTargetHasComboPoint(GameObject target, float comboPoints)
    {
        var state = target.GetComponent<CharacterState>();
        if (isServer)
        {
            for (int i = 0; i < comboPoints - 1; i++)
            {
                state?.AddState(States.Knockdown, comboPoints, 0, _hero.gameObject, name);
            }
        }
    }

    private float _pendingFireDamageBonus = 0f;
    private float _pendingScorchedSoulChance = 0f;
    
    #region Сonst
    private const float HitsInRowResetDelay = 2f;
    private const float MinDirectionSqrMagnitude = 0.0001f;
    private const float KnockdownDurationDefault = 13f;
    private const float KnockdownDurationCombo = 6f;
    private const float MaxHitsInRow = 4f;
    private const float SearchTargetInRadius = 0.5f;
    #endregion

    private static readonly int KickTrigger = Animator.StringToHash("KickAA");

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => KickTrigger;

    protected override bool IsCanCast => Targeting.GetTarget() != null && Vector3.Distance(Targeting.GetTarget().Transform.position, transform.position) <= AreaInfo.Radius && Targeting.NoObstacles(Targeting.GetTarget().Transform.position, transform.position, _obstacle);
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
        _castTarget = null;
        _wasDamageApplied = false;
        Targeting.ClearTarget();
        Targeting.ClearTempTarget();
        //_target = null;
        Hero.Move.StopLookAt();
        _hero.Move.SetCanMove(true);
        AnimCastEnded();
        if (_hitsInRowCoroutine != null) StopCoroutine(_hitsInRowCoroutine);
    }
    
    public void AddFireBonus(float damagePercent, float scorchedChance)
    {
        _pendingFireDamageBonus += damagePercent;
        _pendingScorchedSoulChance += scorchedChance;
    }

    public void Kick_ScorpionMoveFalse()
    {
        if (_hero == null || _hero.Move == null) return;

        var target = _castTarget != null ? Targeting.GetTarget()?.Character : _lastTarget;
        if (target == null)
        {
            _hero.Move.StopLookAt();
            return;
        }


        _hero.Move.StopMoveAndAnimationMove();
        _hero.Move.SetCanMove(false);

        Vector3 direction = target.transform.position - _hero.transform.position;
        bool badDirection = float.IsInfinity(target.transform.position.x) || direction.sqrMagnitude < MinDirectionSqrMagnitude;

        if (badDirection)
        {
            _hero.Move.StopLookAt();
            return;
        }

        _hero.Move.LookAtPosition(Targeting.GetTarget().Character.transform.position);
    }

    public void Kick_ScorpionMoveTrue()
    {
        _hero.Move.SetCanMove(true);
        Hero.Move.StopLookAt();
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        _wasDamageApplied = false;

        while (Targeting.GetTempTarget()?.Targetable == null)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget(Targeting.GetMousePoint(), SearchTargetInRadius);

                if (Targeting.GetTempTarget()?.Targetable != null && Targeting.GetTempTarget()?.Targetable is IDamageable damageable)
                {
                    if (IsAllyTarget(damageable) || damageable as Character == Hero) Targeting.ClearTempTarget();

                    else
                    {
                        if (Targeting.GetTempTarget()?.Targetable is Character character && character.SelectedCircle != null) character.SelectedCircle.IsActive = false;
                        break;
                    }
                }
            }
            yield return null;
        }

        Targeting.SetTarget(Targeting.GetTempTarget()?.Targetable);
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Targeting.GetTarget()?.Targetable);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (_castTarget == null) yield break;;
        if (!IsCanCast) yield break;

        if (_hitsInRowCoroutine != null) StopCoroutine(_hitsInRowCoroutine);
        _hero.Move.LookAtTransform(Targeting.GetTempTarget()?.Targetable.Transform);
        _lastTarget = _castTarget as Character;

        ApplyAttackDamageKick();
    }

    private void ApplyAttackDamageKick()
    {
        if (_wasDamageApplied || _castTarget == null) return;

        var targetGO = (_castTarget as MonoBehaviour)?.gameObject;
        if (targetGO == null) return;

        if (Vector3.Distance(_hero.transform.position, targetGO.transform.position) > AreaInfo.Radius)
            return;

        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(_damageValue),
            Type = Info.DamageType,
            School = Schools.Physical
        };

        _wasDamageApplied = true;

        CmdApplyDamage(targetGO, damage,0);
        
        float bonus = _pendingFireDamageBonus;
        float scorchedChance = _pendingScorchedSoulChance;
        _pendingFireDamageBonus = 0f;
        _pendingScorchedSoulChance = 0f;

        Damage additionalDamage = new Damage
        {
            Value = damage.Value * bonus,
            Type = Info.DamageType,
            School = Schools.Fire
        };

        if(additionalDamage.Value > 0)
            CmdApplyDamage(targetGO, additionalDamage, scorchedChance);
    }

    private IEnumerator HitsInRowTimer()
    {
        yield return _waitForHitsInRowTimer;
        _kickHitsInRow = 0;
        _hitsInRowCoroutine = null;
    }

    private void AttackPassed(Character target)
    {
        OnDamaged?.Invoke(target.gameObject,this);
        
        if (_hitsInRowCoroutine != null)
            StopCoroutine(_hitsInRowCoroutine);

        _hitsInRowCoroutine = StartCoroutine(HitsInRowTimer());

        var state = target.GetComponent<CharacterState>();
        float chance = BaseKnockdownChance;
        if (_scorpionPassive.IsAddStateUpdateChance)
        {
            if (state.CheckForState(States.DisappointmentState))
            {
                chance += _scorpionPassive.AdditionalAddStateChance;
            }
        }
        if (_isKick_ScorpionRowTalent)
        {
            _kickHitsInRow++;

            chance += _kickHitsInRow * KnockDownPerHit;
        }
        else
        {
            _kickHitsInRow = 0;
        }
        
        if (Random.value <= chance)
        {
            state?.AddState(States.Knockdown, KnockdownDurationDefault, 0, _hero.gameObject, name);
        }

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
    private void CmdApplyDamage(GameObject target, Damage damage, float scorchedChance)
    {
        OnBeforeApplyParticipatingDamage?.Invoke(ref damage,this,target);
        if (target == null) return;
        var damageable = target.GetComponent<IDamageable>();
        if (damageable == null) return;
        
        bool isHit = damageable.TryTakeDamage(ref damage, this);

        if (isHit && damageable is Character character)
        {
            AttackPassed(character);
            if (scorchedChance > 0f && Random.Range(0f, 100f) <= scorchedChance)
                character.CharacterState.AddState(States.ScorchedSoul, 5f, 0f, _hero.gameObject, name);
        }
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
        if (targetInfo.GetTargets().Count > 0)
        {
            Targeting.SetTarget(targetInfo.GetTargets()[0]);
            _castTarget = Targeting.GetTarget()?.Damageable;
        }
    }

    protected override void ClearData()
    {
        _wasDamageApplied = false;
        Targeting.ClearTarget();
        Targeting.ClearTempTarget();
        _hero.Move.StopLookAt();
        AnimCastEnded();
        if (_hitsInRowCoroutine != null) StopCoroutine(_hitsInRowCoroutine);
    }
}
