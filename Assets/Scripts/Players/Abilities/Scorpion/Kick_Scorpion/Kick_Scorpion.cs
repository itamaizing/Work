using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Kick_Scorpion : AutoAttackSkill
{
    [Header("Ability settings")]
    [SerializeField] private Character _playerLinks;
    [SerializeField] private Sub_LavaPool_Scorpion _pool;
    [SerializeField] private PassiveCombo_Scorpion _comboCounter;
    [SerializeField][Range(0, 100)] private float _minDamage = 10f;
    [SerializeField][Range(0, 100)] private float _maxDamage = 15f;

    [Header("Debug info")] 

    [SerializeField][Range(0f, 1f)] private float _debuffApplyChance = 0.1f;
    [SerializeField][ReadOnly] private byte _counterRow = 1;

    private Coroutine _hitsInRowCoroutine;
    private Character _lastTarget = null;

    public float DamageRange => Random.Range(_minDamage, _maxDamage);

    protected override void CastAction()
    {
        if (_lastTarget != null && _lastTarget != _target) //�����
        {
            _comboCounter.ResetCounter();
            //_playerLinks.Combo_Player.RemoveAll();
        }

        if (_hitsInRowCoroutine != null)
        {
            StopCoroutine(_hitsInRowCoroutine);
            _hitsInRowCoroutine = null;
        }

        Debug.Log(transform.position);
        Debug.Log(_target.transform.position);

        if (Vector2.Distance(LastTargetPosition, _target.transform.position) <= 2f)
        {
            Damage damage = new Damage
            {
                Value = Buff.Damage.GetBuffedValue(DamageRange),
                Type = DamageType,
            };
            CmdAttack(damage, _target.gameObject);
        }
        _lastTarget = _target;
    }
    private void AttackPassed(Transform target)
    {
        Debug.LogWarning("Kick_Scorpion .AttackPassed - �����");

        _comboCounter.AddAbility(target, ScorpionAbility.Kick);

        _counterRow *= 2;
        _hitsInRowCoroutine = StartCoroutine(HitsInRowTimer());

        if (Random.value <= Mathf.Clamp01(_debuffApplyChance * _counterRow))
        {
            //CmdApplyDebuff(_target.transform);
            _target.GetComponent<CharacterState>().CmdAddState(States.Knockdown, 6f, 0, _hero.gameObject, name);
            _counterRow = 1;
        }
    }
    private void AttackMissed()
    {
        Debug.LogWarning("Kick_Scorpion .AttackMissed - ������");

        _comboCounter.ResetCounter();
    }


    private IEnumerator HitsInRowTimer()
    {
        yield return new WaitForSeconds(CastDeley + 1f);

        _counterRow = 1;

        _hitsInRowCoroutine = null;
    }

    [Command]
    private void CmdAttack(Damage damage, GameObject hp)
    {
        if (_tempTargetForDamage != hp.transform)
        {
            _tempTargetForDamage = hp.transform;
            _tempHPForDamage = hp.GetComponent<Health>();
        }

        bool result = _tempHPForDamage.TryTakeDamage(ref damage, this);
        RpcSelfNotifyHitResult(result, _tempTargetForDamage);

    }

    [TargetRpc]
    private void RpcSelfNotifyHitResult(bool state, Transform target)
    {
        if (state)
        {
            AttackPassed(target);
        }
        else
        {
            AttackMissed();
        }
    }
}
