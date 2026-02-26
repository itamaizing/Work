using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class LightningStrike : Skill
{
    [SerializeField] private ParticleSystem _particlePref;
    [SerializeField, Range(0, 100)] private int _debuffChance = 15;

    protected override bool IsCanCast { get => CheckCanCast(); }

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;

    private bool CheckCanCast()
    {
        return Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position) <= AreaInfo.Radius && Targeting.GetTarget()?.Character != null;
    }

    public void AnimCastLight()
    {
        AnimStartCastCoroutine();
    }

    public void AnimLightEnd()
    {
        AnimCastEnded();
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        Targeting.SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
    }

    protected override IEnumerator CastJob()
    {
        if (Targeting.GetTarget()?.Character != null)
        {
            Damage damage = new Damage
            {
                Value = Buff.Damage.GetBuffedValue(Damage),
                Type = DamageType
            };
            CmdApplyDamage(damage, Targeting.GetTarget()?.Character.gameObject);

            CmdCreateParticle(Targeting.GetTarget().Character.Position);

            if (UnityEngine.Random.Range(1, 100) <= _debuffChance)
            {
                Targeting.GetTarget()?.Character.CharacterState.AddState(States.Discharge, 2, 0, Hero.gameObject, name);
            }
            Targeting.ClearTarget();
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
            }
            yield return null;
        }

        targetInfo.AddTarget(Targeting.GetTarget()?.Character);
        callbackDataSaved(targetInfo);
    }

    private void CreateParticle(Vector3 position)
    {
        if (_particlePref != null)
        {
            GameObject item = Instantiate(_particlePref.gameObject, position, Quaternion.identity);
        }
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
