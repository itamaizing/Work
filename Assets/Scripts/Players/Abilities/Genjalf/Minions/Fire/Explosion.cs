using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class Explosion : Skill
{
    [SerializeField] private ParticleSystem _particlePref;

    private Character _target;

    protected override bool IsCanCast { get => CheckCanCast(); }

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;

    private bool CheckCanCast()
    {
        return
               Vector3.Distance(_target.transform.position, transform.position) <= Radius;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _target = (Character)targetInfo.Targets[0];
    }

    protected override IEnumerator CastJob()
    {
        if (_target != null)
        {
            int stacks = _target.CharacterState.GetState(States.Burning).CurrentStacksCount;

            Damage damage = new Damage
            {
                Value = stacks * Buff.Damage.GetBuffedValue(Damage),
                Type = DamageType,
                PhysicAttackType = AttackRangeType,
            };
            CmdApplyDamage(damage, _target.gameObject);

            CmdCreateParticle(_target.Position);
        }
        yield return null;
    }

    protected override void ClearData()
    {
        _target = null;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        while (_target == null)
        {
            if (GetMouseButton)
            {
                //_target = GetRaycastTarget();
            }
            yield return null;
        }

        targetInfo.Targets.Add(_target);
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
