using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class StreamOfIcyWater : MoveSkill
{
    [SerializeField] private GameObject _effect;

    [SerializeField] private float _breakCastDistance = 0.5f;

    //private Character _target;

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;
    
    private float _clickRadius = 0.5f;
    
    private bool IsEnemyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Enemy");

    public void AnimCastStreamOfIcyWater()
    {
        AnimStartCastCoroutine();
    }

    public void AnimStreamOfIcyWaterEnd()
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
        _hero.Animator.SetTrigger("Attack02");
        _hero.NetworkAnimator.SetTrigger("Attack02");

        float time = 0;
        CmdSetActiveParticle(true);

        float initialDistance = Vector3.Distance(transform.position, Targeting.GetTarget().Character.Position);

        while (time < _channelComponent.CastDuration)
        {
            _effect.transform.localScale = new Vector3(_effect.transform.localScale.x, _effect.transform.localScale.y, Vector3.Distance(transform.position, Targeting.GetTarget().Character.Position));

            yield return new WaitForSeconds(_channelComponent.TickInterval);
            Damage damage = new Damage
            {
                Value = Buff.Damage.GetBuffedValue(Damage),
                Type = Info.DamageType,
                PhysicAttackType = Info.AttackRangeType,
            };
            CmdApplyDamage(damage, Targeting.GetTarget()?.Character.gameObject);
            
            CmdAddState(Targeting.GetTarget()?.Character.gameObject);

            time += _channelComponent.TickInterval;

            yield return null;

            if (Vector3.Distance(transform.position, Targeting.GetTarget().Character.Position) > initialDistance + _breakCastDistance)
                break;
        }

        TryCancel(true);
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
            {
                Vector3 clickPoint = Targeting.GetMousePoint();
        
                Targeting.FindTempTarget(clickPoint, _clickRadius, canTargetSelf: false);
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
        targetInfo.AddTarget(Targeting.GetTempTarget()?.Character);
        Targeting.ClearTempTarget();
        targetDataSavedCallback(targetInfo);
    }

    [Command]
    private void CmdAddState(GameObject target)
    {
        if(target != null)
            target.GetComponent<Character>().CharacterState.AddState(States.Cooling, 6, 0, Hero.gameObject, "Minion");
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
