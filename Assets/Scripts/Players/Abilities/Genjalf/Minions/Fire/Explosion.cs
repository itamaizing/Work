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
               Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position) <= AreaInfo.Radius;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        Targeting.SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
    }

    protected override IEnumerator CastJob()
    {
        if (Targeting.GetTarget()?.Character != null)
        {
            int stacks = Targeting.GetTarget().Character.CharacterState.GetState(States.Burning).CurrentStacksCount;

            Damage damage = new Damage
            {
                Value = stacks * Buff.Damage.GetBuffedValue(Damage),
                Type = Info.DamageType,
                PhysicAttackType = Info.AttackRangeType,
            };
            CmdApplyDamage(damage, Targeting.GetTarget()?.Character.gameObject);

            CmdCreateParticle(Targeting.GetTarget().Character.Position);
        }
        yield return null;
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
        //_target = null;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        while (Targeting.GetTarget()?.Character == null)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget();
                //_target = GetRaycastTarget();
            }
            yield return null;
        }

        targetInfo.GetTargets().Add(Targeting.GetTarget()?.Character);
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
