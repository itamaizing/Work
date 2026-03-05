using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class Impatica : Skill
{
    [SerializeField] private Character _playerLinks;
    [SerializeField] private float duration;

    protected override bool IsCanCast => IsHaveCharge && GetTargetCharacter() != null;
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
        if (value) Chargers += 1;
        else Chargers -= 1;
    }

    #endregion

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (GetTempTargetCharacter() == null)
        {
            if (GetMouseButton)
            {
                FindTargetCharacter(TargetSearchRadius, GetMousePoint());

                if (GetTempTargetCharacter() != null)
                {
                    if (IsAllyTarget(GetTempTargetCharacter()) || GetTempTargetCharacter() == Hero)
                    {
                        ClearTempTarget();
                    }
                    else
                    {
                        GetTempTargetCharacter().SelectedCircle.IsActive = true;
                        break;
                    }
                }
            }
            yield return null;
        }

        SetTarget(GetTempTargetCharacter());
        ClearTempTarget();
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(GetTargetCharacter());
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (GetTargetCharacter() != null)
        {
            CmdApplyImpaticaState(GetTargetCharacter().gameObject);

        }

        yield return null;
    }

    protected override void ClearData()
    {
        ClearTarget();
        ClearTempTarget();
        //_target = null;
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
        if (targetInfo.GetTargets().Count > 0) SetTarget((targetInfo.GetTargets()[0] as Character));
    }
}