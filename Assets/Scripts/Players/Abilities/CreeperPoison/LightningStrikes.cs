using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class LightningStrikes : AutoAttackAbility
{
    [SerializeField] CreeperStrike creeperStrike;

    private float _attackSpeedDeacrease = 10f;
    private float _attackSpeedStrikes;
    private float originalAttackSpeed;
    private float _cooldownStrikes;
    private int _countStrikes = 2;

    private bool _enabled = false;
    private bool _canCast = true;
    private bool _enemyInRadius = false;
    public bool _isUsing = false;

    private new void Start()
    {
        creeperStrike = GetComponent<CreeperStrike>();
    }

    private void Update()
    {
        Timer();

        if (_cooldownStrikes <= 0 && _isUsing)
        {
            CastAction();
        }
        
        if (Input.GetMouseButtonDown(1))
        {
            Cancel();
        }
    }

    protected override void CastAction()
    {
        _isUsing = true;
        _enabled = true;
        DecreaseAttackSpeed(_attackSpeedStrikes);
    }
    protected override void Cancel()
    {
        _enabled = false;
    }

    public void DecreaseAttackSpeed(float _attackSpeedStrikes)
    {
        if (creeperStrike.CurrentTarget != null)
        {
            _enemyInRadius = true;
            if (_enemyInRadius)
            {
                creeperStrike.OriginalAttackSpeed = creeperStrike.AttackSpeed;
                _attackSpeedStrikes = creeperStrike.CurrentAttackSpeed / _attackSpeedDeacrease;

                creeperStrike.ModifyAttackSpeed(_attackSpeedStrikes);

                for (int i = 0; i < _countStrikes; i++)
                {
                    StartCoroutine(creeperStrike.UseAbilityCoroutine());
                }
            }
        }
        creeperStrike.ResetAttackSpeed();
        _canCast = false;
        _isUsing = false;
        _enemyInRadius = false;
        Cancel();
    }

    private void Timer()
    {
        _cooldownStrikes = _cooldown;
        _cooldownStrikes -= Time.deltaTime;
        if (_cooldownStrikes <= 0)
        {
            _canCast = true;
        }
    }
}