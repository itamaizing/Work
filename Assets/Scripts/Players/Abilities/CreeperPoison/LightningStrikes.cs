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
    
    private Character _currentTarget;

    private float _animTime;
    private float _cooldownMultiplier = 2f;
    private float _heatedGlandsDuration = 4f;
    private float _radiusSearchTarget = 0.5f;

    private bool _isUsedLightningStrikes = false;
    private bool _isIncreaseCooldownTime = false;
    private bool _isCanDamageDeal = false;

    public bool IsUsedLightningStrikes { get => _isUsedLightningStrikes; set => _isUsedLightningStrikes = value; }
    public bool IsCanDamageDeal { get => _isCanDamageDeal; set => _isCanDamageDeal = value; }

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("LightningStrikesAttacking");

    protected override bool IsCanCast
    {
        get
        {
            Character target = _currentTarget != null ? _currentTarget : Targeting.GetTarget()?.Character;
            if (target == null) return false;
            return Targeting.NoObstacles(target.transform.position, _obstacle) && Targeting.IsTargetInRadius(AreaInfo.Radius, target.transform);
        }
    }

    private bool IsAllyTarget(IDamageable target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");

    public event Action OnLightningStrikesEnd;

    protected override void Awake()
    {
        base.Awake();
    }

    public void AnimLightningStrikesCast()
    {
        AnimStartCastCoroutine();
    }

    public void AnimLightningStrikesEnd()
    {
        OnLightningStrikesEnd?.Invoke();
        AnimCastEnded();
    }
    /* public void Targeting.SetTarget(Character target)
     {
         _target = target;
     }*/

    protected override void ClearData()
    {
        _currentTarget = null;

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

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
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
            AnimCastEnded();
            yield break;
        }

        if (_lightningMovement != null && _lightningMovement.IsInMovement)
        {
            _animTime = GetClipLength();
            IncreaseAnimSpeed();
        }

        Debug.Log("LightningStrikes / CastAction");

        if (_coldBlood != null && _coldBlood.IsCanCritLightningStrikes && _isIncreaseCooldownTime == false)
        {
            float newCooldownTime = Cooldown.BaseCooldownTime * _cooldownMultiplier;
            Cooldown.CooldownTime = newCooldownTime;

            _isIncreaseCooldownTime = true;
        }
        else
        {
            Cooldown.CooldownTime = Cooldown.BaseCooldownTime;
        }

        DamageDeal(target);

        yield return null;
    }

    private bool IsTargetInRange(Character target)
    {
        if (target == null) return false;
        return Vector3.Distance(_player.transform.position, target.transform.position) <= AreaInfo.Radius;
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
            float multiplier = _lightningMovement.DurationLeap - 4.9f; // �������� �������� (���������� - 0.1)
            float animTimeMultiplier = _animTime / multiplier;
            Debug.Log("LightningStrikes / multiplier = " + animTimeMultiplier);
            _player.Animator.SetFloat("LightningStrikesMultiplierSpeedAnimation", animTimeMultiplier);
        }
    }

    private void DamageDeal(Character target)
    {
        if (target == null) return;
        Debug.Log("LightningStrikes / DamageDeal");

        if (_player.Abilities.LastCastedSkill is CreeperStrike) _player.Abilities.PreviewCastedSkill = this;
        _player.Abilities.LastCastedSkill = this;
        _creeperStrike.DamageDeal(target, true);

        _isCanDamageDeal = false;
        if (_coldBlood != null && _coldBlood.IsCanCritLightningStrikes && _isIncreaseCooldownTime) _isIncreaseCooldownTime = false;
    }
}
