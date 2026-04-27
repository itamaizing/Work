using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class Explosion : MoveSkill
{
    [SerializeField] private ParticleSystem _particlePref;

    //private Character _target;

    protected override bool IsCanCast { get => CheckCanCast(); }
    
    private bool IsEnemyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Enemy");

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    private float _clickRadius = 0.5f;
    private float _particleLifetime = 1.5f;

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
            foreach (var states in Targeting.GetTarget().Character.CharacterState.CurrentStates)
            {
                Debug.LogError(states.State);
            }
            var state = Targeting.GetTarget().Character.CharacterState.GetState(States.Burning);
            if (state == null) yield break;
            
            int stacks = state.CurrentStacksCount;

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
        while (Targeting.GetTempTarget() == null)
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
    }

    private void CreateParticle(Vector3 position)
    {
        Destroy(Instantiate(_particlePref.gameObject, position, Quaternion.identity),_particleLifetime);
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
