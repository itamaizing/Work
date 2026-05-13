using Mirror;
using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class NewPunch_Scorpion : Skill, IComboParticipatingSkill
{
    [Header("Ability settings")]
    [SerializeField] private Character _playerLinks;
    [SerializeField] private ScorpionPassive scorpionPassive;
    [SerializeField] private byte _hitsInRow = 1;

    private float _pendingFireDamageBonus = 0f;
    private float _pendingScorchedSoulChance = 0f;
    
    private Coroutine _hitsInRowCoroutine;
    private Animator _animator;
    private bool _isRightKick = true;
    private bool _wasDamageApplied = false;
    private WaitForSeconds _waitForMinHitsForWarmingUp;

    private Character _lastTarget;
    private Character _currentTarget;
    
    public event Action<GameObject, Skill> OnDamaged;

    #region Constants
    private const float MinDirectionSqrMagnitude = 0.0001f;
    private const float HitsInRowResetDelay = 2f;
    private const int MinHitsForWarmingUp = 2;
    private const float StunDuration = 1f;
    private const float SearchTargetInRadius = 1f;
    #endregion

    private static readonly int RightPunchTrigger = Animator.StringToHash("RightPunch");
    private static readonly int LeftPunchTrigger = Animator.StringToHash("LeftPunch");

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => _isRightKick ? RightPunchTrigger : LeftPunchTrigger;

    protected override bool IsCanCast => Targeting.GetTarget() != null && Vector3.Distance(Targeting.GetTarget().Transform.position, transform.position) <= AreaInfo.Radius && Targeting.NoObstacles(Targeting.GetTarget().Transform.position, transform.position, _obstacle);
    private bool IsAllyTarget(IDamageable target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");

    private void Start()
    {
        _animator = GetComponent<Animator>();
        _waitForMinHitsForWarmingUp = new WaitForSeconds(MinHitsForWarmingUp);
    }
    
    public void AddFireBonus(float damagePercent, float scorchedChance)
    {
        _pendingFireDamageBonus += damagePercent;
        _pendingScorchedSoulChance += scorchedChance;
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
    private bool _isWarningUpAddState;

    public void WarningUpAddState(bool value)
    {
        _isWarningUpAddState = value;
    }

    #endregion

    private bool IsTargetInRange() { return Targeting.GetTarget() != null && Vector3.Distance(_playerLinks.transform.position, Targeting.GetTarget().Transform.position) <= AreaInfo.Radius; }

    private void HandleSkillCanceled()
    {
        _wasDamageApplied = false;
        Targeting.ClearTarget();
        Targeting.ClearTempTarget();
        //_target = null;
        Hero.Move.StopLookAt();
        _hero.Move.SetCanMove(true);
        if (_hitsInRowCoroutine != null) StopCoroutine(_hitsInRowCoroutine);
        AnimCastEnded();
    }

    public void NewPunch_ScorpionMoveFalse()
    {
        if (_hero == null || _hero.Move == null) return;

        var target = Targeting.GetTarget() != null ? Targeting.GetTarget()?.Targetable : _lastTarget;
        if (target == null)
        {
            _hero.Move.StopLookAt();
            return;
        }


        _hero.Move.StopMoveAndAnimationMove();
        _hero.Move.SetCanMove(false);

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
                        _hero.Move.LookAtTransform(Targeting.GetTempTarget()?.Targetable.Transform);
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
        if (Targeting.GetTarget() == null) yield return null;
        if (!IsTargetInRange()) yield return null;

        _isRightKick = !_isRightKick;
        _lastTarget = Targeting.GetTarget()?.Character;

        ApplyAttackDamage();

        yield return null;
    }

    private void ApplyAttackDamage()
    {
        if (_wasDamageApplied) return;

        var targetData = Targeting.GetTarget();
        if (targetData == null) return;

        var target = targetData.Targetable as IDamageable;
        if (target == null) return;

        if (Vector3.Distance(_hero.transform.position, targetData.Transform.position) > AreaInfo.Radius)
            return;

        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(_damageValue),
            Type = Info.DamageType,
        };

        _wasDamageApplied = true;

        CmdApplyDamage(target.gameObject, damage,0);
        
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
            CmdApplyDamage(target.gameObject, additionalDamage, scorchedChance);
    }

    [Command]
    private void CmdApplyDamage(GameObject target, Damage damage, float scorchedChance)
    {
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
        OnDamaged?.Invoke(target.gameObject,this);
        
        if (_hitsInRowCoroutine != null)
            StopCoroutine(_hitsInRowCoroutine);
        _hitsInRowCoroutine = StartCoroutine(HitsInRowTimer());

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
        if (targetInfo.GetTargets().Count > 0) Targeting.SetTarget(targetInfo.GetTargets()[0]);
    }

    protected override void ClearData()
    {
        _wasDamageApplied = false;
        Targeting.ClearTarget();
        Targeting.ClearTempTarget();
        _hero.Move.StopLookAt();
        if (_hitsInRowCoroutine != null) StopCoroutine(_hitsInRowCoroutine);
        AnimCastEnded();
    }

    private IEnumerator HitsInRowTimer()
    {
        yield return _waitForMinHitsForWarmingUp;
        _hitsInRow = 0;
        _hitsInRowCoroutine = null;
    }

    public void OnFinalComboSkill(GameObject target)
    {
        var state = target.GetComponent<CharacterState>();
        if(isServer)
            state?.AddState(States.Stun, StunDuration, 0, _hero.gameObject, name);
    }

    public void OnTargetHasComboPoint(GameObject target, float comboPoints)
    {
        var state = target.GetComponent<CharacterState>();
        if (isServer)
        {
            Debug.LogError("ComboPoints: " + comboPoints);
            state?.AddState(States.Stun, comboPoints, 0, _hero.gameObject, name);
        }
    }

    //private void AttackMissed()
    //{
    //    Debug.Log("[NewPunch_Scorpion] Attack Missed");
    //    _comboCounter?.ResetCounter();
    //}
}
