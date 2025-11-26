using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class Explosion : Skill
{
    [SerializeField] private ParticleSystem _particlePref;

    //private Character _target;

    protected override bool IsCanCast { get => CheckCanCast(); }

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;

    private bool CheckCanCast()
    {
        return
               Vector3.Distance(GetTarget().transform.position, transform.position) <= Radius;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        SetTarget((Character)targetInfo.GetTargets()[0]);
    }

    protected override IEnumerator CastJob()
    {
        if (GetTarget() != null)
        {
            int stacks = GetTarget().CharacterState.GetState(States.Burning).CurrentStacksCount;

            Damage damage = new Damage
            {
                Value = stacks * Buff.Damage.GetBuffedValue(Damage),
                Type = DamageType,
                PhysicAttackType = AttackRangeType,
            };
            CmdApplyDamage(damage, GetTarget().gameObject);

            CmdCreateParticle(GetTarget().Position);
        }
        yield return null;
    }

    protected override void ClearData()
    {
        ClearTarget();
        //_target = null;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        while (GetTarget() == null)
        {
            if (GetMouseButton)
            {
                FindTarget();
                //_target = GetRaycastTarget();
            }
            yield return null;
        }

        targetInfo.GetTargets().Add(GetTarget());
        callbackDataSaved(targetInfo);
    }

    private void CreateParticle(Vector3 position)
    {
        GameObject item = Instantiate(_particlePref.gameObject, position, Quaternion.identity);
    }

    [Command]
    protected void CmdCreateParticle(Vector3 position)
    {
        RpcCreateParticle(position);
    }

    [ClientRpc]
    private void RpcCreateParticle(Vector3 position)
    {
        CreateParticle(position);
    }
}
