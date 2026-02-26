using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class StreamOfIcyWater : Skill
{
    [SerializeField] private GameObject _effect;

    //private Character _target;

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;

    public void AnimCastStreamOfIcyWater()
    {
        AnimStartCastCoroutine();
    }

    public void AnimStreamOfIcyWaterEnd()
    {
        AnimCastEnded();
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        Targeting.SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
    }

    protected override IEnumerator CastJob()
    {
        _hero.Animator.SetTrigger("Attack02");
        _hero.NetworkAnimator.SetTrigger("Attack02");

        float time = 0;
        CmdSetActiveParticle(true);

        while (time < CastStreamDuration)
        {
            _effect.transform.localScale = new Vector3(_effect.transform.localScale.x, _effect.transform.localScale.y, Vector3.Distance(transform.position, Targeting.GetTarget().Character.Position));

            yield return new WaitForSeconds(_manaCostRate);
            Damage damage = new Damage
            {
                Value = Buff.Damage.GetBuffedValue(Damage),
                Type = Info.DamageType,
                PhysicAttackType = Info.AttackRangeType,
            };
            CmdApplyDamage(damage, Targeting.GetTarget()?.Character.gameObject);
			Targeting.GetTarget()?.Character.CharacterState.CmdAddState(States.Frosting, 6, 0, Hero.gameObject, name);

            time += _manaCostRate;

            yield return null;
        }
        ClearData();
    }

    protected override void ClearData()
    {
        AnimStreamOfIcyWaterEnd();
        CmdSetActiveParticle(false);
        //_target = null;
        Targeting.ClearTarget();
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        //Character target = null;

        TargetInfo targetInfo = new();

        while (Targeting.GetTarget()?.Character == null)
        {
            if (GetMouseButton)
                Targeting.FindTempTarget();
               // target = GetRaycastTarget();

            yield return null;
        }

        Hero.Move.LookAtPosition(Targeting.GetTarget().Character.Position);
        targetInfo.AddTarget(Targeting.GetTarget()?.Character);
        targetDataSavedCallback.Invoke(targetInfo);
        yield return null;
    }


    [Command]
    private void CmdSetActiveParticle(bool status)
    {
        ClientRpcSetActiveParticle(status);
    }

    [ClientRpc]
    private void ClientRpcSetActiveParticle(bool status)
    {
        _effect.gameObject.SetActive(status);
    }
}
