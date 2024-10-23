using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CleavingBlade_Scorpion : Skill
{
    [Header("Ability settings")]
    [SerializeField] private PassiveCombo_Scorpion _comboCounter;
    [SerializeField][Range(0, 100)] private float _minDamage = 23f;
    [SerializeField][Range(0, 100)] private float _maxDamage = 26f;
    [SyncVar]
    private int _counter = 1;
    private Character _target;
    public float DamageRange => Random.Range(_minDamage, _maxDamage);
    protected override bool IsCanCast
    {
        get
        {
            if (_target != null)
                return Vector3.Distance(_target.transform.position, transform.position) <= Radius;

            return false;
        }
    }

    protected override int AnimTriggerCastDelay => throw new System.NotImplementedException();

    protected override int AnimTriggerCast => throw new System.NotImplementedException();

    private void ResetValue()
    {

    }

    private void AttackPassed(bool shouldIncreaseCounter, Transform target)
    {
        Debug.LogWarning("CleavingBlade_Scorpion .AttackPassed - Попал");
        _comboCounter.AddAbility(target, ScorpionAbility.Blade);

        if (shouldIncreaseCounter)
        {
            if (_counter == 3)
            {
                _counter = 1;
            }
            else
            {
                _counter++;
            }
        }
        _target.GetComponent<CharacterState>().CmdAddState(States.Bleeding, 6f, 0, _hero.gameObject, name);

        _target = null;
    }
    private void AttackMissed()
    {
        Debug.LogWarning("CleavingBlade_Scorpion .AttackMissed - Промах");
        _counter = 1;
        _comboCounter.ResetCounter();

        _target = null;
    }

    protected override IEnumerator PrepareJob()
    {
        while (_target == null)
        {
            if (GetMouseButton)
            {
                _target = GetRaycastTarget(true);
            }
            yield return null;
        }
    }

    protected override IEnumerator CastJob()
    {
        float initialCastDelay = CastDeley;
        //if (_counter == 2) CastDeley *= 0.8f;

        if(_counter <= 2)
        {
            TryAttack(true, 1f);
        }
        else
        {
            TryAttack(false, 0.75f);
            yield return new WaitForSeconds(0.3f);
            TryAttack(false, 0.75f);
            _counter = 1;
        }

        yield return null;
    }
    
    protected override void ClearData()
    {
        //_target = null;
    }

    private void TryAttack(bool shouldIncreaseCounter, float damageMultiplier)
    {
        if (_target != null && Vector2.Distance(transform.position, _target.transform.position) <= 2f + 2f + 0.19f)
        {
            Damage damage = new Damage
            {
                Value = Buff.Damage.GetBuffedValue(DamageRange * damageMultiplier),
                Type = DamageType,
                Range = AttackRangeType,
            };

            CmdAttack(damage, _target.gameObject, shouldIncreaseCounter);
        }
    }


    [Command]
    private void CmdAttack(Damage damage, GameObject hp, bool shouldIncreaseCounter)
    {
        if (_tempTargetForDamage != hp.transform)
        {
            _tempTargetForDamage = hp.transform;
            _tempHPForDamage = hp.GetComponent<Health>();
        }

        bool result = _tempHPForDamage.TryTakeDamage(ref damage, this);
        RpcSelfNotifyHitResult(result, shouldIncreaseCounter, _tempTargetForDamage);

    }

    [TargetRpc]
    private void RpcSelfNotifyHitResult(bool state, bool shouldIncreaseCounter, Transform target)
    {
        if (state)
        {
            AttackPassed(shouldIncreaseCounter, target);
        }
        else
        {
            AttackMissed();
        }
    }
}
