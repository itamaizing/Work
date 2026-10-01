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

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

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

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
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
        var targetCharacter = targetGameObject.GetComponent<Character>();
        if (targetCharacter != null)
        {
            targetCharacter.CharacterState.AddState(States.Impatience, duration, 0, _playerLinks.gameObject, name);
        }
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0) Targeting.SetTarget((targetInfo.GetTargets()[0] as Character));
    }
}