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
    private float _timeBaff = 4f;

    [Header("Abillity Components")]
    [SerializeField] private LightningMovement _lightningMovement;
    [SerializeField] private CreeperStrike _creeperStrike;
    [SerializeField] private Character _player;

    private Character _currentTarget;

    private int _countStrikes = 2;
    private float _attackSpeedDeacrease = 0.1f;
    private bool _isUsedLightningStrikes = false;

    private Coroutine _useCoroutine;

    public bool IsUsedLightningStrikes => _isUsedLightningStrikes;

    protected override void ClearData()
    {
        base.ClearData();

        if (_useCoroutine != null)
        {
            StopCoroutine(UseAbilityCoroutine());
            _useCoroutine = null;
        }

        if (_isUsedLightningStrikes)
        {
            Invoke("ResetUsedLightningStrikes", 4f);
        }
    }

    protected override IEnumerator PrepareJob()
    {
        if (_lightningMovement.IsInMovement)
        {
            IsCanDamageDeal = true;
            yield break;
        }
        else
        {
            base.PrepareJob();
        }
    }

    protected override void CastAction()
    {
        _currentTarget = _target;
        _useCoroutine = StartCoroutine(UseAbilityCoroutine());
    }

    public void UseLightningStrikes(Character target)
    {
        DecreaseAttackSpeed(target);
    }

    private void ResetUsedLightningStrikes()
    {
        _isUsedLightningStrikes = false;
        IsCanDamageDeal = false;
    }

    private IEnumerator UseAbilityCoroutine()
    {
        _isUsedLightningStrikes = true;
        DecreaseAttackSpeed(_currentTarget);
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
                if (_heatedGlands.IsActive)
                {
                    _player.CharacterState.CmdAddState(States.HeatedGlands, _timeBaff, 0, _player.gameObject, Name);
                }
                _creeperStrike.CurrentCountHit = 0;
            }

            _creeperStrike.Buff.AttackSpeed.ReductionPercentage(_attackSpeedDeacrease);
        }
    }
}