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
        return Vector3.Distance(GetTargetCharacter().transform.position, transform.position) <= Radius && GetTargetCharacter() != null;
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
        SetTarget((Character)targetInfo.GetTargets()[0]);
    }

    protected override IEnumerator CastJob()
    {
        if (GetTargetCharacter() != null)
        {
            Damage damage = new Damage
            {
                Value = Buff.Damage.GetBuffedValue(Damage),
                Type = DamageType
            };
            CmdApplyDamage(damage, GetTargetCharacter().gameObject);

            CmdCreateParticle(GetTargetCharacter().Position);

            if (UnityEngine.Random.Range(1, 100) <= _debuffChance)
            {
                GetTargetCharacter().CharacterState.AddState(States.Discharge, 2, 0, Hero.gameObject, name);
            }
            ClearTarget();
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

        while (GetTargetCharacter() == null)
        {
            if (GetMouseButton)
            {
                FindTargetCharacter();
            }
            yield return null;
        }

        targetInfo.AddTarget(GetTargetCharacter());
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
