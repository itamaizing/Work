using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Impatica : Skill
{
    [SerializeField] private Character _playerLinks;
    [SerializeField] private float duration;

    protected override bool IsCanCast
    {
        get
        {
            Character target = Targeting.GetTarget()?.Character;

            if (target == null)
                return false;

            if (!IsAllyTarget(target))
                return false;

            if (target == Hero)
                return false;

            float distance = Vector3.Distance(Hero.transform.position, target.transform.position);

            return distance <= AreaInfo.Radius;
        }
    }
    
    public override bool IsHaveResources =>
        IsHaveResourceOnSkill &&
        (Charges.UsesCharges ? Charges.RemainingCharges > 0 : !Cooldown.IsActive);

    private bool IsAllyTarget(IDamageable target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");

    private const float TargetSearchRadius = 0.5f;

    protected override int AnimTriggerPrepare => 0;
    protected override int AnimTriggerCast => 0;

    private uint _nextImpaticaId;
    
    public bool IsExtendDamageAbsorption { get => _isExtendDamageAbsorption; set => _isExtendDamageAbsorption = value; }

    #region Talent
    private bool _isExtendDamageAbsorption = false;

    public void ExtendDamageAbsorption(bool value) => _isExtendDamageAbsorption = value;
    

    private bool _secondChargeActive;
    private double? _pendingSecondChargeEndTime;

    public void SecondCharge(bool value)
    {
        if (_secondChargeActive == value) return;
        _secondChargeActive = value;

        if (value)
        {
            Charges.EnableChargers(true, 2, Cooldown.BaseCooldownTime);

            double slot1End = NetworkTime.time;

            if (Cooldown.IsActive)
            {
                float remaining1 = Cooldown.RemainingTime;
                slot1End = NetworkTime.time + remaining1;

                Charges.StartRecharge(remaining1);
                Cooldown.ForceEnd();
            }

            if (_pendingSecondChargeEndTime.HasValue)
            {
                double duration2 = _pendingSecondChargeEndTime.Value - slot1End;

                if (duration2 > 0)
                    Charges.StartRecharge((float)duration2);

                _pendingSecondChargeEndTime = null;
            }
        }
        else
        {
            if (RechargeTimers.Count > 0)
            {
                double firstEnd = RechargeTimers[0];

                _pendingSecondChargeEndTime = RechargeTimers.Count > 1
                    ? RechargeTimers[RechargeTimers.Count - 1]
                    : null;

                for (int i = RechargeTimers.Count - 1; i >= 0; i--)
                    Charges.RestoreCharge(i);

                double remainingFirst = firstEnd - NetworkTime.time;
                if (remainingFirst > 0)
                    Cooldown.StartCustom((float)remainingFirst);
            }
            else
            {
                _pendingSecondChargeEndTime = null;
            }

            Charges.EnableChargers(false, 0, Cooldown.BaseCooldownTime);
        }
    }

    protected override void UseCooldownOrCharges()
    {
        if (Charges.UsesCharges) Charges.TryUse();
        else Cooldown.Start();
    }

    #endregion

    protected override IEnumerator TargetingJob(Action<TargetInfo> callbackDataSaved)
    {
        while (Targeting.GetTempTarget()?.Character == null)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget(Targeting.GetMousePoint(), TargetSearchRadius);

                if (Targeting.GetTempTarget()?.Character != null)
                {
                    Character tempTarget = Targeting.GetTempTarget().Character;

                    if (!IsAllyTarget(tempTarget) || tempTarget == Hero)
                    {
                        Targeting.ClearTempTarget();
                    }

                    else
                    {
                        tempTarget.SelectedCircle.IsActive = true;
                        break;
                    }
                }
            }

            yield return null;
        }

        Character selectedTarget = Targeting.GetTempTarget()?.Character;
        Targeting.ClearTempTarget();
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(selectedTarget);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (Targeting.GetTarget()?.Character != null)
        {
            CmdApplyImpaticaState(Targeting.GetTarget()?.Character.gameObject);

        }

        yield return null;
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
        Targeting.ClearTempTarget();
    }

    [Command]
    private void CmdApplyImpaticaState(GameObject targetGameObject)
    {
        if (targetGameObject == null) return;

        var target = targetGameObject.GetComponent<Character>();
        if (target == null || target.IsDead) return;

        var state = CreateLocalImpatica(target, duration);
        if (state == null) return;

        state.ApplyNetworkSnapshot(this, ++_nextImpaticaId, NetworkTime.time + state.RemainingDuration, state.MaxDuration);

        SendImpaticaSnapshot(state);
    }

    private ImpatienceStateStacking CreateLocalImpatica(Character target, float remaining)
    {
        var states = target.CharacterState;
        int previousCount = states.CurrentStates.Count;

        states.AddStateLogic(States.Impatience, Mathf.Max(remaining, 0.001f), 0f, Schools.None, _playerLinks.gameObject, name, isCanDodgeMagState: !isServer);

        if (states.CurrentStates.Count <= previousCount)
            return null;

        return states.CurrentStates[states.CurrentStates.Count - 1] as ImpatienceStateStacking;
    }

    private ImpatienceStateStacking FindImpatica(Character target, uint id)
    {
        foreach (var current in target.CharacterState.CurrentStates)
        {
            if (current is ImpatienceStateStacking state && state.NetworkId == id && state.SourceCaster == _playerLinks)
                return state;
        }

        return null;
    }

    [Server]
    public void SendImpaticaSnapshot(ImpatienceStateStacking state)
    {
        if (state.Target == null || state.NetworkId == 0)
            return;

        RpcSyncImpatica(state.Target.gameObject, state.NetworkId, NetworkTime.time + state.RemainingDuration, state.MaxDuration);
    }

    [ClientRpc]
    private void RpcSyncImpatica(GameObject targetObject, uint id, double endTime, float maxDuration)
    {
        if (isServer || targetObject == null) return;

        var target = targetObject.GetComponent<Character>();
        if (target == null) return;

        var state = FindImpatica(target, id);

        if (state == null)
        {
            state = CreateLocalImpatica(target, (float)(endTime - NetworkTime.time));
        }

        state?.ApplyNetworkSnapshot(this, id, endTime, maxDuration);
    }

    [Server]
    public void SendImpaticaRemoval(ImpatienceStateStacking state)
    {
        if (state.Target == null) return;

        RpcRemoveImpatica(state.Target.gameObject, state.NetworkId);
    }

    [ClientRpc]
    private void RpcRemoveImpatica(GameObject targetObject, uint id)
    {
        if (isServer || targetObject == null) return;

        var target = targetObject.GetComponent<Character>();
        if (target == null) return;

        FindImpatica(target, id)?.ExitState();
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0) Targeting.SetTarget((targetInfo.GetTargets()[0] as Character));
    }
}