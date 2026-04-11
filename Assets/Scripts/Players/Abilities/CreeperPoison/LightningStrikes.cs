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
    
    //private Character _currentTarget;

    private float _animTime;
    private float _cooldownMultiplier = 2f;
    private float _heatedGlandsDuration = 4f;
    private float _radiusSearchTarget = 0.5f;

    private bool _isUsedLightningStrikes = false;
    private bool _isIncreaseCooldownTime = false;
    private bool _isCanDamageDeal = false;

    public float BaseCooldownTime { get => _baseCooldownTime; }
    public bool IsUsedLightningStrikes { get => _isUsedLightningStrikes; set => _isUsedLightningStrikes = value; }
    public bool IsCanDamageDeal { get => _isCanDamageDeal; set => _isCanDamageDeal = value; }

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("LightningStrikesAttacking");

    protected override bool IsCanCast
    {
        get
        {
            if (Targeting.GetTarget() == null)return false;
            return Targeting.NoObstacles(Targeting.GetTarget().Transform.position, _obstacle) && Targeting.IsTargetInRadius(AreaInfo.Radius, Targeting.GetTarget().Transform);
        }
    }

    private bool IsAllyTarget(IDamageable target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");

    public event Action OnLightningStrikesEnd;

    protected override void Awake()
    {
        base.Awake();

        _baseCooldownTime = Cooldown.BaseCooldownTime;
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
        Targeting.ClearTarget();
        Targeting.ClearTempTarget();
        _hero.Move.StopLookAt();
    }

    public void ClearDataLightningStrikes() => ClearData();

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo?.GetTargets()?.Count > 0) Targeting.SetTarget(targetInfo.GetTargets()[0]);
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
        if (Targeting.GetTarget() == null || !IsTargetInRange())
        {
            AnimCastEnded();
            yield break;
        }

        if (_lightningMovement.IsInMovement)
        {
            _animTime = GetClipLength();
            IncreaseAnimSpeed();
        }

        Debug.Log("LightningStrikes / CastAction");

        if (_coldBlood.IsCanCritLightningStrikes && _isIncreaseCooldownTime == false)
        {
            float newCooldownTime = _baseCooldownTime * _cooldownMultiplier;
            Cooldown.CooldownTime = newCooldownTime;

            _isIncreaseCooldownTime = true;
        }
        else
        {
            Cooldown.CooldownTime = _baseCooldownTime;
        }

        /*if (_currentTarget == null)
            _currentTarget = _target;*/

        DamageDeal();

        yield return null;
    }

    private bool IsTargetInRange()
    {
        return Targeting.GetTarget() != null && Vector3.Distance(_player.transform.position, Targeting.GetTarget().Transform.position) <= AreaInfo.Radius;
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

    private void DamageDeal()
    {
        Debug.Log("LightningStrikes / DamageDeal");

        if (_player.Abilities.LastCastedSkill is CreeperStrike) _player.Abilities.PreviewCastedSkill = this;
        _player.Abilities.LastCastedSkill = this;
        _creeperStrike.DamageDeal(Targeting.GetTarget()?.Character, true);

       _isCanDamageDeal = false;

        //if (_heatedGlands.Data.IsOpen)
        //    _player.CharacterState.CmdAddState(States.HeatedGlands, _heatedGlandsDuration, 0, _player.gameObject, null);

        if (_coldBlood.IsCanCritLightningStrikes && _isIncreaseCooldownTime == true) _isIncreaseCooldownTime = false;
    }
}
