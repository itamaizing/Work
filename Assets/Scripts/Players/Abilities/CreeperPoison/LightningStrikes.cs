using System.Collections;
using UnityEngine;

public class LightningStrikes : AutoAttackSkill
{
    public bool IsCanDamageDeal = false;

    [Header("Talents")]
    [SerializeField] private HeatedGlands _heatedGlands;
    [SerializeField] private KillersStamina _killersStamina; 
    private float _timeBaff = 4f;

    [Header("Abillity Components")]
    [SerializeField] private ColdBlood _coldBlood;
    [SerializeField] private LightningMovement _lightningMovement;
    [SerializeField] private CreeperStrike _creeperStrike;
    [SerializeField] private Character _player;

    private Character _currentTarget;

    private float _animTime;
    private float _cooldownMultiplier = 2f;
    
    private bool _isUsedLightningStrikes = false;
    private bool _isIncreaseCooldownTime = false;

    private Coroutine _useCoroutine;
    private Coroutine _damageDealCoroutine;

    public bool IsUsedLightningStrikes => _isUsedLightningStrikes;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerAutoAttack => Animator.StringToHash("LightningStrikesAttacking");

    public void AnimLightningStrikesCast()
    {
        AnimCastAction();
    }

    public void AnimLightningStrikesEnd()
    {
        AnimCastEnded();
    }

    public void UseLightningStrikesOfLightningMovement()
    { 
        //AnimLightningStrikesCast();
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

    protected override IEnumerator PrepareJob()
    {
        if (_lightningMovement.IsInMovement)
        {
            _animTime = GetClipLength();
            IsCanDamageDeal = true;
            IncreaseAnimSpeed();
            _target = _lightningMovement.Target;
        }

        return base.PrepareJob();
    }

    protected override void ClearData()
    {
        base.ClearData();

        if (_animTime > 0)
            _player.Animator.speed = _animTime;

        if (_useCoroutine != null)
        {
            StopCoroutine(_useCoroutine);
            _useCoroutine = null;
        }

        if (_damageDealCoroutine != null)
        {
            StopCoroutine(_damageDealCoroutine); 
            _damageDealCoroutine = null;
        }

        if (_isUsedLightningStrikes)
        {
            Invoke("ResetUsedLightningStrikes", 1.3f);
        }
    }

    protected override void CastAction()
    {
        Debug.Log("LightningStrikes / CastAction");

        if (_coldBlood.IsCanCritLightningStrikes && _isIncreaseCooldownTime == false)
        {
            float newCooldownTime = _cooldownTime * _cooldownMultiplier;
            this.IncreaseSetCooldown(newCooldownTime);

            _isIncreaseCooldownTime = true;
        }

        if (_currentTarget == null)
            _currentTarget = _target;

        DamageDeal();
    }

    private void IncreaseAnimSpeed()
    {
        // Изменить в аниматоре скорость через Анимация -> параметр -> multiplier
        if (_animTime > 0)
        {
            float multiplier = _lightningMovement.DurationLeap;
            float animTimeMultiplier = _animTime / multiplier - 1f;

            _player.Animator.speed = animTimeMultiplier;
        }
    }

    private void ResetUsedLightningStrikes()
    {
        _isUsedLightningStrikes = false;
    }

    private void DamageDeal()
    {
        Debug.Log("LightningStrikes / DamageDeal");

        //_creeperStrike.CurrentTarget = _currentTarget;
        _creeperStrike.DamageDeal(_currentTarget);

        if (_lightningMovement.IsInMovement)
        {
            IsCanDamageDeal = false;
        }

        _creeperStrike.CurrentCountHit = 0;
    }
}