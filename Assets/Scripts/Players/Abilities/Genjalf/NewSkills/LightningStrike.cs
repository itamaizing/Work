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

    private float _clickRadius = 0.5f;
    
    private bool IsEnemyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Enemy");

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
                Type = Info.DamageType
            };
            CmdApplyDamage(damage, Targeting.GetTarget()?.Character.gameObject);

            CmdCreateParticle(Targeting.GetTarget().Character.Position);

            if (UnityEngine.Random.Range(1, 100) <= _debuffChance)
            {
                CmdAddState(Targeting.GetTarget()?.Character);
            }
            Targeting.ClearTarget();
        }
        yield return null;
    }
    
    [Command] private void CmdAddState(Character target) => target.CharacterState.AddState(States.Discharge, 2, 0,Hero.gameObject, name);

    protected override void ClearData()
    {
        Targeting.ClearTarget();
        //_target = null;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        while (Targeting.GetTempTarget()?.Character == null)
        {
        
            if (GetMouseButton)
            {
                Vector3 clickPoint = Targeting.GetMousePoint();
                
                Targeting.FindTempTarget(clickPoint, _clickRadius, canTargetSelf: true);
                
                if (Targeting.GetTempTarget()?.Character is Character character)
                {
                    if (character != null && !IsEnemyTarget(character))
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
        Targeting.SetTarget(Targeting.GetTempTarget()?.Character);
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
