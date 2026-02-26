using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;



public class FireBoll : Skill
{
    [SerializeField] private Projectile _projectile;

    //private Character _target;

    protected override bool IsCanCast { get => CheckCanCast(); }

    protected override int AnimTriggerCastDelay => Animator.StringToHash("SpellDaley");

    protected override int AnimTriggerCast => Animator.StringToHash("Attack04");

    private bool CheckCanCast()
    {
        return 
               Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position) <= AreaInfo.Radius;
    }

    public void AnimCastFireboll()
    {
        AnimStartCastCoroutine();
    }

    public void AnimFirebollEnd()
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
            CmdCreateProjecttile(Targeting.GetTarget()?.Character.gameObject);
        }
        yield return null;
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
        //_target = null;
        Hero.Move.StopLookAt();
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        while (Targeting.GetTarget()?.Character == null)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget();
              //  _target = GetRaycastTarget();
            }
            yield return null;
        }
        
        targetInfo.AddTarget(Targeting.GetTarget()?.Character);
        callbackDataSaved(targetInfo);

        this.CastStarted += OnCastStarted;
    }

    private void OnCastStarted()
    {
        Hero.Move.LookAtTransform(Targeting.GetTarget()?.Character.transform);
        this.CastStarted -= OnCastStarted;
    }

    [Command]
    protected void CmdCreateProjecttile(GameObject target)
    {
        GameObject item = Instantiate(_projectile.gameObject, transform.position + Vector3.up, Quaternion.identity);

        SceneManager.MoveGameObjectToScene(item, _hero.NetworkSettings.MyRoom);

        var projectile = item.GetComponent<Projectile>();

        projectile.EndPointReached += OnEndPointReached;
        projectile.StartFly(target.transform, true);

        NetworkServer.Spawn(item);
    }

    private void OnEndPointReached(Projectile arg0, GameObject target)
    {
        arg0.EndPointReached -= OnEndPointReached;
        TargetRpcOnEndPointReached(target);
    }

    [TargetRpc]
    private void TargetRpcOnEndPointReached(GameObject target)
    {
        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(Damage),
            Type = Info.DamageType,
            PhysicAttackType = Info.AttackRangeType,
        };
        CmdApplyDamage(damage, target);
        target.GetComponent<Character>().CharacterState.CmdAddState(States.Burning, 6, 0, Hero.gameObject, name);
    }
}
