using System;
using System.Collections;
using UnityEngine;

public class LightningStrikes : Skill
{
    [Header("Talents")]
    //[SerializeField] private HeatedGlands _heatedGlands;
    //[SerializeField] private KillersStamina _killersStamina; 
    
    [Header("Abillity Components")]
    [SerializeField] private ColdBlood _coldBlood;
    [SerializeField] private LightningMovement _lightningMovement;
    [SerializeField] private CreeperStrike _creeperStrike;
    [SerializeField] private Character _player;

    private const int HitsPerCast = 2;
    private const float FallbackAttackDuration = 3f;
    private static readonly int attackTrigger = Animator.StringToHash("LightningStrikesAttacking");

    private Character _currentTarget;

    private float _animTime;
    private float _cooldownMultiplier = 2f;
    private float _radiusSearchTarget = 0.5f;

    private bool _isUsedLightningStrikes = false;
    private bool _isIncreaseCooldownTime = false;
    private bool _isCanDamageDeal = false;

    private int _hitsDealt;
    private bool _awaitingHits;
    private bool _attackFinished;

    public bool IsUsedLightningStrikes { get => _isUsedLightningStrikes; set => _isUsedLightningStrikes = value; }
    public bool IsCanDamageDeal { get => _isCanDamageDeal; set => _isCanDamageDeal = value; }

    protected override int AnimTriggerPrepare => 0;
    protected override int AnimTriggerCast => 0;

    protected override bool IsCanCast
    {
        get
        {
            Character target = _currentTarget != null ? _currentTarget : Targeting.GetTarget()?.Character;
            if (target == null) return false;
            return Targeting.NoObstacles(target.transform.position, Targeting.AdditionalObstacle) && Targeting.IsTargetInRadius(AreaInfo.Radius, target.transform);
        }
    }

    private bool IsAllyTarget(IDamageable target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");

    public event Action OnLightningStrikesEnd;

    protected override void Awake()
    {
        base.Awake();
    }

    #region Animation events

    public void AnimLightningStrikesCast()
    {
        if (!_awaitingHits || _hitsDealt >= HitsPerCast) return;

        _hitsDealt++;
        DealHit(_currentTarget, isLastHit: _hitsDealt >= HitsPerCast);
    }

    public void AnimLightningStrikesEnd()
    {
        OnLightningStrikesEnd?.Invoke();
        _attackFinished = true;
        AnimCastEnded();
    }

    #endregion

    protected override void ClearData()
    {
        _currentTarget = null;
        _awaitingHits = false;

        Targeting.ClearTarget();
        Targeting.ClearTempTarget();
        _hero.Move.StopLookAt();
    }

    public void ClearDataLightningStrikes() => ClearData();

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _currentTarget = null;

        if (targetInfo == null) return;
        if (targetInfo.GetTargets().Count == 0) return;

        _currentTarget = targetInfo.GetTargets()[0] as Character;

        if (_currentTarget == null) return;

        Targeting.SetTarget(_currentTarget);
        Hero.Move.LookAtTransform(_currentTarget.transform);
    }

    protected override IEnumerator TargetingJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        while (Targeting.GetTempTarget()?.Targetable == null)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget(Targeting.GetMousePoint(), _radiusSearchTarget);

                if (Targeting.GetTempTarget()?.Targetable != null && Targeting.GetTempTarget()?.Targetable is IDamageable damageable)
                {
                    if (IsAllyTarget(damageable) || damageable as Character == Hero) Targeting.ClearTempTarget();
                    else break;
                }
            }
            yield return null;
        }

        Targeting.SetTarget(Targeting.GetTempTarget()?.Targetable);

        targetInfo.Points.Add(Targeting.GetTarget().Transform.position);
        targetInfo.AddTarget(Targeting.GetTarget()?.Targetable);
        callbackDataSaved.Invoke(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        Character target = _currentTarget;

        if (target == null || !IsTargetInRange(target))
        {
            TryCancel(true);
            yield break;
        }
        
        if (_lightningMovement != null && _lightningMovement.IsInMovement)
        {
            _animTime = GetClipLength();
            IncreaseAnimSpeed();
        }

        if (_coldBlood != null && _coldBlood.IsCanCritLightningStrikes && _isIncreaseCooldownTime == false)
        {
            Cooldown.CooldownTime = Cooldown.BaseCooldownTime * _cooldownMultiplier;
            _isIncreaseCooldownTime = true;
        }
        else
        {
            Cooldown.CooldownTime = Cooldown.BaseCooldownTime;
        }

        if (_player.Abilities.LastCastedSkill is CreeperStrike) _player.Abilities.PreviewCastedSkill = this;
        _player.Abilities.LastCastedSkill = this;

        _hitsDealt = 0;
        _attackFinished = false;
        _awaitingHits = true;

        _hero.Animator.SetFloat(HashAnimPlayer.CastSpeed, GetCastSpeed());
        _hero.Animator.SetTrigger(attackTrigger);
        _hero.NetworkAnimator.SetTrigger(attackTrigger);

        float timeout = GetAttackTimeout();
        float elapsed = 0f;
        while (!_attackFinished)
        {
            elapsed += Time.deltaTime;
            if (elapsed > timeout)
            {
                break;
            }
            yield return null;
        }

        _awaitingHits = false;
    }

    private bool IsTargetInRange(Character target)
    {
        if (target == null) return false;
        return Vector3.Distance(_player.transform.position, target.transform.position) <= AreaInfo.Radius;
    }

    private float GetAttackTimeout()
    {
        float clip = GetClipLength();
        float duration = clip > 0f ? clip : FallbackAttackDuration;
        return duration / Mathf.Max(0.1f, GetCastSpeed()) + 1f;
    }

    private float GetClipLength()
    {
        RuntimeAnimatorController animController = _player.Animator.runtimeAnimatorController;
        foreach (var clip in animController.animationClips)
        {
            if (clip.name == "LightningStrikesAttack")
            {
                return clip.length;
            }
        }
        return -1f;
    }

    private void IncreaseAnimSpeed()
    {
        if (_animTime > 0)
        {
            float multiplier = _lightningMovement.DurationLeap - 4.9f;
            float animTimeMultiplier = _animTime / multiplier;
            _player.Animator.SetFloat("LightningStrikesMultiplierSpeedAnimation", animTimeMultiplier);
        }
    }

    private void DealHit(Character target, bool isLastHit)
    {
        if (target == null) return;

        _creeperStrike.DamageDeal(target, true);
        _isCanDamageDeal = false;

        if (isLastHit && _coldBlood != null && _coldBlood.IsCanCritLightningStrikes && _isIncreaseCooldownTime)
            _isIncreaseCooldownTime = false;
    }
}