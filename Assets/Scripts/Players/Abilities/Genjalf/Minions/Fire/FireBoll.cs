using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;



public class FireBoll : MoveSkill
{
    [SerializeField] private Projectile _projectile;
    [SerializeField] private float _debuffTime = 7;
    
    protected override bool IsCanCast { get => CheckCanCast(); }
    private bool IsEnemyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Enemy");

    protected override int AnimTriggerCastDelay => Animator.StringToHash("SpellDaley");

    protected override int AnimTriggerCast => Animator.StringToHash("Attack04");
    
    private float _clickRadius = 0.5f;
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
    
    private void OnEnable()
    {
        Canceled += CancelMove;
    }

    private void OnDisable()
    {
        Canceled -= CancelMove;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        Targeting.SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
        
        if (!IsCanCast)
        {
            MoveTo();
        }
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
                Vector3 clickPoint = Targeting.GetMousePoint();
        
                Targeting.FindTempTarget(clickPoint, _clickRadius, canTargetSelf: false);
                if (Targeting.GetTempTarget()?.Character is Character character)
                {
                    if (Targeting.GetTempTarget()?.Character != null && !IsEnemyTarget(character))
                    {
                        Targeting.ClearTempTarget();
                    }
                    else
                    {
                        if (character.SelectedCircle != null) character.SelectedCircle.IsActive = false;
                        break;
                    }
                }
            }
            yield return null;
        }
        targetInfo.AddTarget(Targeting.GetTempTarget()?.Character);
        Targeting.ClearTempTarget();
        callbackDataSaved(targetInfo);
        
        CastStarted += OnCastStarted;
    }

    private void OnCastStarted()
    {
        Hero.Move.LookAtTransform(Targeting.GetTarget()?.Character.transform);
        CastStarted -= OnCastStarted;
    }

    [Command]
    protected void CmdCreateProjecttile(GameObject target)
    {
        GameObject item = Instantiate(_projectile.gameObject, transform.position + Vector3.up, Quaternion.identity);

        //SceneManager.MoveGameObjectToScene(item, _hero.NetworkSettings.MyRoom);

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
        CmdState(target,_debuffTime);
    }
    
    [Command]
    private void CmdState(GameObject enemy, float time)
    {
        Character enemyChar = enemy.GetComponent<Character>();
        enemyChar.CharacterState.AddState(States.Burning, time, 0,Schools.Fire, gameObject, name);
    }
}
