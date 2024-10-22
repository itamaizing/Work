using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class LightningStrikes : AutoAttackSkill
{
    public bool Enabled;
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

    private float _attackSpeedDeacrease = 0.1f;
    private float _cooldownMultiplier = 2f;

    private bool _isUsedLightningStrikes = false;
    private bool _isIncreaseCooldownTime = false;

    private Coroutine _useCoroutine;

    public bool IsUsedLightningStrikes => _isUsedLightningStrikes;

    public void UseLightningStrikesOfLightningMovement(Character target, float duration)
    {
        _useCoroutine = StartCoroutine(UseAbilityCoroutine(target));
        Invoke("UseRecharge", duration);
    }

    private void UseRecharge()
    {
        TryPayCost();
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
        Debug.Log("LightningStrikes / ClearData");
        base.ClearData();

        if (_useCoroutine != null)
        {
            StopCoroutine(UseAbilityCoroutine(_currentTarget));
            _useCoroutine = null;
        }

        if (_isUsedLightningStrikes)
        {
            Invoke("ResetUsedLightningStrikes", 2f);
        }
    }

    protected override void CastAction()
    { 
        if (_coldBlood.IsCanCritLightningStrikes && !_isIncreaseCooldownTime)
        {
            float newCooldownTime = _cooldownTime * _cooldownMultiplier;
            this.IncreaseSetCooldown(newCooldownTime);

            Debug.Log("Cooldown LightningStrikes == " + _cooldownTime);
            _isIncreaseCooldownTime = true;
        }
        Debug.Log("LightningStrikes / CastAction");
        _currentTarget = _target;
        _useCoroutine = StartCoroutine(UseAbilityCoroutine(_currentTarget));
    }

    private void ResetUsedLightningStrikes()
    {
        _isUsedLightningStrikes = false;
        IsCanDamageDeal = false;
    }

    private IEnumerator UseAbilityCoroutine(Character target)
    {
        _isUsedLightningStrikes = true;
        DecreaseAttackSpeed(target);
        yield return null;
    }

    private void DecreaseAttackSpeed(Character target)
    {
        if (target != null)
        {
            _creeperStrike.Buff.AttackSpeed.IncreasePercentage(_attackSpeedDeacrease);

            for (int i = 0; i < _countStrikes; i++)
            {
                _creeperStrike.DealingDamageFromHits(target);

                if (_heatedGlands.Data.IsOpen)
                {
                    _player.CharacterState.CmdAddState(States.HeatedGlands, _timeBaff, 0, _player.gameObject, Name);
                }

                _creeperStrike.CurrentCountHit = 0;
            }

            _creeperStrike.Buff.AttackSpeed.ReductionPercentage(_attackSpeedDeacrease);

            if (_coldBlood.IsCanCritLightningStrikes)
            {
                Debug.Log("if _absoluteAccucary.ISCanCritLightningStrikes");
                _coldBlood.IsCanCritLightningStrikes = false;
                _coldBlood.IsCanCritCreeperStrike = false;
                _isIncreaseCooldownTime = false;
            }
        }
    }
}