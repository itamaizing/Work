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

    public event IComboParticipatingSkill.OnBeforeApplyDamageDelegate OnBeforeApplyParticipatingDamage;
    public event Action<GameObject, Skill> OnDamaged;

    #region Constants
    private const float MinDirectionSqrMagnitude = 0.0001f;
    private const float HitsInRowResetDelay = 2f;
    private const int MinHitsForWarmingUp = 2;
    private const float StunDuration = 1f;
    private const float SearchTargetInRadius = 0.5f;
    #endregion

    private static readonly int RightPunchTrigger = Animator.StringToHash("RightPunch");
    private static readonly int LeftPunchTrigger = Animator.StringToHash("LeftPunch");

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => _isRightKick ? RightPunchTrigger : LeftPunchTrigger;

    private IDamageable _castTarget;
    
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

    public void StunningAddChance(bool value)
    {
        if(value == _isStunningAddChance) return;
        _isStunningAddChance = value;
    }

    [Header("WarmingUp  talent")]
    [SerializeField] private float warmingUpDuration;
    private bool _isWarningUpAddState;

    public void WarningUpAddState(bool value)
    {
        if(_isWarningUpAddState == value) return;
        _isWarningUpAddState = value;
        if(isClient)
            CmdWarningUpAddState(value);
    }

    private bool _isWarmingUpHealingIncrease;
    public void WarmingUpHealingIncrease(bool value)
    {
        if(value == _isWarmingUpHealingIncrease) return;
        _isWarmingUpHealingIncrease = value;
        if(isClient)
            CmdWarmingUpHealingIncrease(value);
    }

    [Command]
    private void CmdWarningUpAddState(bool value)
    {
        if(_isWarningUpAddState == value) return;
        _isWarningUpAddState = value;
    }

    [Command]
    private void CmdWarmingUpHealingIncrease(bool value)
    {
        if(value == _isWarmingUpHealingIncrease) return;
        _isWarmingUpHealingIncrease = value;
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

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
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
        if (_castTarget == null) yield break;
        if (!IsTargetInRange()) yield break;
        _hero.Move.LookAtTransform(Targeting.GetTempTarget()?.Targetable.Transform);
        _isRightKick = !_isRightKick;
        _lastTarget = Targeting.GetTarget()?.Character;

        ApplyAttackDamage();

        yield return null;
    }

    private void ApplyAttackDamage()
    {
        if (_wasDamageApplied) return;
        if (_castTarget == null) return;

        var target = (_castTarget as MonoBehaviour)?.gameObject;
        if (target == null) return;

        if (Vector3.Distance(_hero.transform.position, target.transform.position) > AreaInfo.Radius)
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

        if (additionalDamage.Value > 0)
        {
            CmdApplyDamage(target.gameObject, additionalDamage, scorchedChance);
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
                character.CharacterState.AddState(States.ScorchedSoul, 5f, 0f, Schools.Fire, _hero.gameObject, name);
        }
    }

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
            if(!_isWarmingUpHealingIncrease)
                state.AddState(States.WarmingUpState, warmingUpDuration, 0, Schools.Physical, _hero.gameObject, nameof(WarmingUpState));
            else
                state.AddState(States.WarmingUpState, warmingUpDuration, 0, Schools.Physical, _hero.gameObject, nameof(WarmingUpState)+"HealingIncrease");
                
            _hitsInRow = 0;
        }

        if (_isStunningAddChance)
        {
            var state = target.GetComponent<CharacterState>();
            var chance = stunningAddChance;
            if (scorpionPassive.IsAddStateUpdateChance && state != null)
            {
                if (state.CheckForState(States.DisappointmentState))
                {
                    chance += scorpionPassive.AdditionalAddStateChance;
                }
            }
            if (UnityEngine.Random.value <= chance) 
                state?.AddState(States.Stun, StunDuration, 0,Schools.Physical, _hero.gameObject, name);
        }
    }

    public void NewPunch_ScorpionCast()
    {
        if (_wasDamageApplied) return;
        AnimStartCastCoroutine();
    }

    public void NewPunch_ScorpionEnded()
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
        _castTarget = null;
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
        if (isServer)
        {
            state?.AddState(States.Stun, StunDuration, 0, Schools.Physical, _hero.gameObject, "final");
        }
    }

    public void OnTargetHasComboPoint(GameObject target, float comboPoints)
    {
        var state = target.GetComponent<CharacterState>();
        if (isServer && comboPoints > 0)
        {
            float stunDuration = comboPoints * StunDuration;
        
            state?.AddState(States.Stun, stunDuration, 0, Schools.Physical, _hero.gameObject, "points");
        }
    }

    //private void AttackMissed()
    //{
    //    Debug.Log("[NewPunch_Scorpion] Attack Missed");
    //    _comboCounter?.ResetCounter();
    //}
}
