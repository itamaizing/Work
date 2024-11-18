using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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

    private int _countStrikes = 2;
    private int _countHit = 0;

    private float _animTime;
    private float _attackSpeed = 0.1f;
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

    public void UseLightningStrikesOfLightningMovement(Character target, float duration)
    {
        _useCoroutine = StartCoroutine(UseAbilityCoroutine(target));
    }

    private float GetClipLength()
    {
        RuntimeAnimatorController animController = _player.Animator.runtimeAnimatorController;
        foreach (var clip in animController.animationClips)
        {
            if (clip.name == "LightningStrikesAttack")
            {
                Debug.Log("LightningStrikes / Clip.Name = " + clip.name);
                return clip.length;
            }
        }
        return -1f;
    }

    private void UseRecharge()
    {
        TryPayCost(true);
        if (_useCoroutine != null)
        {
            StopCoroutine(_useCoroutine);
            _useCoroutine = null;
        }
    }

    protected override IEnumerator PrepareJob()
    {
        if (_lightningMovement.IsInMovement)
        {
            IsCanDamageDeal = true;
        }
        return base.PrepareJob();
    }

    protected override void ClearData()
    {
        base.ClearData();

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
        _attackDelay = _attackSpeed;
        _animTime = GetClipLength();

        if (_coldBlood.IsCanCritLightningStrikes && !_isIncreaseCooldownTime)
        {
            float newCooldownTime = _cooldownTime * _cooldownMultiplier;
            this.IncreaseSetCooldown(newCooldownTime);

            _isIncreaseCooldownTime = true;
        }
        _currentTarget = _target;
        _useCoroutine = StartCoroutine(UseAbilityCoroutine(_currentTarget));
    }

    private void ResetUsedLightningStrikes()
    {
        _isUsedLightningStrikes = false;
    }

    private IEnumerator UseAbilityCoroutine(Character target)
    {
        _isUsedLightningStrikes = true;

        if (_damageDealCoroutine == null) 
            _damageDealCoroutine = StartCoroutine(DecreaseAttackSpeed(target));

        yield return null;
    }

    private IEnumerator DecreaseAttackSpeed(Character target)
    {
        _creeperStrike.CurrentTarget = target;
        _countHit = 0;
        yield return null;
        //_creeperStrike.CurrentCountHit = 0;

        //if (_lightningMovement.IsInMovement)
        //{
        //    IsCanDamageDeal = false;
        //    UseRecharge();
        //}

    }
}