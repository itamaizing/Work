using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class Throw_Scorpion : Skill, IComboParticipatingSkill
{
    private float _baseDamage = 15f;
    private float _baseThrowDistance = 1f;
    private float _animationDuration = 1.2f;
    private float _liftHeight = 1.8f;
    
    private float _normalEnergyCost = 40f;
    private float _reducedEnergyCost = 10f;

    private float _currentBonusDamage = 0f;
    private float _currentBonusDistance = 0f;

    public event IComboParticipatingSkill.OnBeforeApplyDamageDelegate OnBeforeApplyParticipatingDamage;
    public event Action<GameObject, Skill> OnDamaged;

    protected override bool IsCanCast => Targeting.GetTarget() != null &&
                                         Vector3.Distance(Targeting.GetTarget().Transform.position, transform.position) <= AreaInfo.Radius &&
                                         CheckResourcesOnSkill();

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("Throw");

    public void AnimThrowStart()
    {
        StartCoroutine(CastJob());
    }

    public void AnimThrowEnd()
    {
        //AnimCastEnded();
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (Targeting.GetTempTarget()?.Character == null)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget(Targeting.GetMousePoint(), 0.5f);

                var temp = Targeting.GetTempTarget()?.Character;
                if (temp != null && temp != Hero)
                    break;
            }
            yield return null;
        }

        TargetInfo info = new TargetInfo();
        info.AddTarget(Targeting.GetTempTarget()?.Character);
        callbackDataSaved(info);
    }
    
    protected override bool CheckResourcesOnSkill()
    {
        var target = Targeting.GetTarget()?.Character;
        if (target == null) return false;

        CharacterState targetState = target.CharacterState;
        bool hasControlDebuff = targetState.CheckForState(States.Stun) || 
                                targetState.CheckForState(States.DisappointmentState);

        float energyCost = hasControlDebuff ? _reducedEnergyCost : _normalEnergyCost;
        var energy = Hero.Resources[ResourceType.Energy];

        return energy != null && energy.CurrentValue >= energyCost;
    }

    protected override IEnumerator CastJob()
    {
        var target = Targeting.GetTarget()?.Character;
        if (target == null) yield break;

        CmdBlockTarget(target.gameObject,false);
        CharacterState targetState = target.CharacterState;

        bool hasControlDebuff = targetState.CheckForState(States.Stun) || 
                                targetState.CheckForState(States.DisappointmentState);
        
        float energyCost = hasControlDebuff ? _reducedEnergyCost : _normalEnergyCost;
        
        Cost.TryPaySingle(energyCost, ResourceType.Energy);

        yield return new WaitForSeconds(_animationDuration / 2);

        CmdExecuteThrow(target.gameObject);

        yield return null;
    }

    [Command]
    private void CmdBlockTarget(GameObject targetObj, bool value)
    {        
        var target = targetObj.GetComponent<Character>();
        if (target == null) return;
        target.Move.SetCanMove(value);
        target.Abilities.SetAbilitiesDisactive(!value);
        TargetBlockMove(targetObj, value);
    }

    [ClientRpc]
    private void TargetBlockMove(GameObject targetObj, bool value)
    {
        var target = targetObj.GetComponent<Character>();
        if (target == null) return;
        target.Move.SetCanMove(value);
        target.Abilities.SetAbilitiesDisactive(!value);
    }
    

    [Command]
    private void CmdExecuteThrow(GameObject targetObj)
    {
        OnDamaged?.Invoke(targetObj, this);
        var target = targetObj.GetComponent<Character>();
        if (target == null) return;
        
        Vector3 throwDirection = (_hero.transform.position - target.transform.position).normalized;
        Vector3 finalPosition = _hero.transform.position + throwDirection * (_baseThrowDistance + _currentBonusDistance);
        
        Vector3 liftPosition = target.transform.position + Vector3.up * _liftHeight;
        target.Move?.RpcDoLiftAndThrow(liftPosition, finalPosition, 1);

        Damage damage = new Damage
        {
            Value = _baseDamage + _currentBonusDamage,
            Type = DamageType.Physical,
            School = Schools.Physical
        };

        ApplyDamage(damage, target.gameObject);

        _currentBonusDamage = 0f;
        _currentBonusDistance = 0f;

        TargetBlockMove(targetObj, true);
    }

    #region Комбо-система

    public void OnFinalComboSkill(GameObject target)
    {
        _currentBonusDamage += 10f;
        _currentBonusDistance += 1f;
    }

    public void OnTargetHasComboPoint(GameObject target, float comboPoints)
    {
        _currentBonusDamage += comboPoints * 10f;
        _currentBonusDistance += comboPoints * 1f;
    }

    #endregion

    protected override void ClearData()
    {
        var currentTarget = Targeting.GetTarget()?.Character;
        if (currentTarget != null)
        {
            CmdBlockTarget(currentTarget.gameObject,true);
        }
        if (_hero != null && _hero.Move != null)
        {
            _hero.Move.StopLookAt();
        }
        base.ClearData();
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0)
            Targeting.SetTarget(targetInfo.GetTargets()[0]);
    }
}
