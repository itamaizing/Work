using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChainLightning : MoveSkill
{
    [SerializeField] private ParticleSystem _particlePref;
    [SerializeField, Range(0, 100)] private int _debuffChance = 15;

    //private Character _target;

    protected override bool IsCanCast { get => CheckCanCast(); }

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;
    
    private float _clickRadius = 0.5f;
    
    private bool IsEnemyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Enemy");

    private void OnEnable()
    {
        Canceled += CancelMove;
    }

    private void OnDisable()
    {
        Canceled -= CancelMove;
    }

    private bool CheckCanCast()
    {
        return Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position) <= AreaInfo.Radius;
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
        
        if (!IsCanCast)
        {
            MoveTo();
        }
    }

    protected override IEnumerator CastJob()
    {
        if (Targeting.GetTarget()?.Character != null)
        {
            Attack(Targeting.GetTarget()?.Character);
            yield return new WaitForSecondsRealtime(0.3f);
            var temps = Physics.OverlapSphere(Targeting.GetTarget().Character.Position, AreaInfo.Radius, Targeting.Layer);
            
            for (int i = 0; i < temps.Length; i++)
            {
                if (i <= 5 && temps[i].TryGetComponent(out Character character))
                {
                    Attack(character);
                    yield return new WaitForSecondsRealtime(0.3f);
                }
            }
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
        Targeting.SetTarget(Targeting.GetTempTarget()?.Targetable);
        Targeting.ClearTempTarget();
        targetInfo.AddTarget(Targeting.GetTarget()?.Character);
        callbackDataSaved(targetInfo);
    }

    private void Attack(Character target)
    {
        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(Damage),
            Type = Info.DamageType,
            PhysicAttackType = Info.AttackRangeType,
        };
        CmdApplyDamage(damage, target.gameObject);
        
        if (UnityEngine.Random.Range(1, 100) <= _debuffChance)
        {
            CmdAddState(Targeting.GetTarget()?.Character);
        }

        CmdCreateParticle(target.Position);
    }

    private void CreateParticle(Vector3 position)
    {
        GameObject item = Instantiate(_particlePref.gameObject, position, Quaternion.identity);
    }
    
    [Command] private void CmdAddState(Character target) => target.CharacterState.AddState(States.Discharge, 2, 0,Hero.gameObject, name);

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
