using System;
using Mirror;
using System.Linq.Expressions;
using UnityEngine;

public class Punch_Scorpion : AutoAttackSkill,IComboParticipatingSkill
{
    [Header("Ability settings")]
    [SerializeField] private Character _playerLinks;
    [SerializeField] private PassiveCombo_Scorpion _comboCounter;
    //[SerializeField] private float _damageValue = 9f;

    private Character _lastTarget = null;
    public event Action<GameObject, Skill> OnDamaged;

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerAutoAttack => throw new System.NotImplementedException();

    protected override void CastAction()
    {
        if (_lastTarget != null && _lastTarget != Targeting.GetTarget()?.Character) //�����
        {
            _comboCounter.ResetCounter();
        }
        Debug.Log(transform.position);
        Debug.Log(Targeting.GetTarget().Character.transform.position);

        //Vector3 closestEnemyPoint = _target.gameObject.GetComponent<CircleCollider2D>().ClosestPoint(transform.position);
        //Vector3 closestMyPoint = transform.parent.parent.GetComponent<CircleCollider2D>().ClosestPoint(_target.transform.position);


        if (Vector2.Distance(LastTargetPosition, Targeting.GetTarget().Character.transform.position) <= 2f)
        {
            Debug.Log("����������� ����������");

            Damage damage = new Damage   
            {
                Value = Buff.Damage.GetBuffedValue(_damageValue),
                Type = Info.DamageType,
            };

            CmdAttack(damage, Targeting.GetTarget()?.Character.gameObject);
        }
        else Debug.LogWarning("������� ������");
        _lastTarget = Targeting.GetTarget()?.Character;

    }
    private void AttackPassed(Character target)
    {
        Debug.LogWarning("Punch_Scorppion .AttackPassed - �����");

        OnDamaged?.Invoke(target.gameObject, this);
    }
    private void AttackMissed()
    {
        Debug.LogWarning("Punch_Scorppion .AttackMissed -������");

        _comboCounter.ResetCounter();
    }

    [Command]
    private void CmdAttack(Damage damage, GameObject hp)
    {
        if (Targeting.ForDamage.Transform != hp.transform)
        {
            Targeting.ForDamage = new TargetData(hp);
        }

        bool result = Targeting.ForDamage.Damageable.TryTakeDamage(ref damage, this);
       //RpcSelfNotifyHitResult(result, _tempTargetForDamageFORRENAME);

    }

    [TargetRpc]
    private void RpcSelfNotifyHitResult(bool state,Character target)
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

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        throw new System.NotImplementedException();
    }
    public void OnFinalComboSkill(GameObject target) { }
    public void OnTargetHasComboPoint(GameObject target, float comboPoints) { }
}
