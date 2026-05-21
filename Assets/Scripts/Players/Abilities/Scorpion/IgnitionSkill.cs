using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class IgnitionSkill : Skill,IFireComboParticipatingSkill
{
    private const float SlowedRegenAmount = 30f;

    private bool _spreadInRingOfFire = true;

    private float _comboPoints = 0;

    protected override bool IsCanCast =>
        Targeting.GetTarget() != null &&
        Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position) <= AreaInfo.Radius;

    protected override int AnimTriggerCastDelay => Animator.StringToHash("CastDelay");
    protected override int AnimTriggerCast => 0;

    private float _clickRadius = 0.5f;

    public void EnableIgnitionSpreadTalent(bool value)
    {
        if(_spreadInRingOfFire == value) return;
        _spreadInRingOfFire = value;
    }
    
    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (Targeting.GetTempTarget()?.Character == null)
        {
            if (GetMouseButton)
            {
                var clickPoint = Targeting.GetMousePoint();
                Targeting.FindTempTarget(clickPoint, _clickRadius, canTargetSelf: false);

                if (Targeting.GetTempTarget()?.Character is Character c && c == Hero)
                    Targeting.ClearTempTarget();
            }
            yield return null;
        }

        Targeting.SetTarget(Targeting.GetTempTarget()?.Character);
        var targetInfo = new TargetInfo();
        targetInfo.AddTarget(Targeting.GetTarget()?.Character);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (Targeting.GetTarget()?.Character == null)
            yield break;

        CmdApplyIgnition(Targeting.GetTarget().Character.gameObject);
        if (_spreadInRingOfFire)
        {
            SpreadIgnitionToRingTargets(Targeting.GetTarget().Character);
        }
        yield return null;
    }

    [Command]
    private void CmdApplyIgnition(GameObject targetGO)
    {
        var targetCharacter = targetGO.GetComponent<Character>();
        if (targetCharacter == null) return;

        targetCharacter.CharacterState.AddState(States.Ignition, 6f, 0f, Hero.gameObject, nameof(IgnitionSkill)+_comboPoints);

        RpcApplyEnergyPenalty();
        _comboPoints = 0;
    }
    
    private void SpreadIgnitionToRingTargets(Character initialTarget)
    {
        var ringAura = Hero.GetComponent<RingOfFireAura>();
        if (ringAura == null || !ringAura.IsActive) 
            return;
        
        if (!ringAura.IsTargetInRing(initialTarget))
            return;
        
        foreach (var target in ringAura.GetCurrentTargets())
        {
            if (target == null || target.IsDead || target == initialTarget) 
                continue;

            CmdApplyIgnition(target.gameObject);
        }
    }
    
    [TargetRpc]
    private void RpcApplyEnergyPenalty()
    {
        if (Hero.Resources.TryGetValue(ResourceType.Energy, out var energy))
            energy.CmdAddRegenModifier(SlowedRegenAmount,2,isFast:false);
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0)
            Targeting.SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
    }

    public void OnFinalComboSkill(GameObject target)
    {
        _comboPoints++;
    }

    public void OnTargetHasComboPoint(GameObject target, float comboPoints)
    {
        _comboPoints += comboPoints;
    }
}
