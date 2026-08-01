using Mirror;
using System;
using System.Collections;
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

    public void SecondCharge(bool value)
    {
        if (value)
        {
            Charges.EnableChargers(true, 2, Cooldown.BaseCooldownTime);
            if (Cooldown.IsActive)
                Charges.StartRecharge(Cooldown.RemainingTime);
        }
        else
        {
            //из-за пинга на сервере пишет 2 - Count, а на клиенте уже 0 - count
            if (Charges.RemainingCharges <= 0 && RechargeTimers.Count > 0)
                Cooldown.StartCustom((float)(RechargeTimers[^1] - NetworkTime.time));

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