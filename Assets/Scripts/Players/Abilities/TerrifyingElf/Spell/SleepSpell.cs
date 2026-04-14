using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class SleepSpell : Skill
{
    [SerializeField] private Character _playerLinks;
    [SerializeField] private float duration;

    //private Character _target;
    //private Character _runtimeTarget;
    private Vector3 _targetPoint = Vector3.positiveInfinity;
    private bool _isSleepInnerDarknessTalentActive = false;

    protected override bool IsCanCast { get => CheckCanCast(); }
    protected override int AnimTriggerCastDelay => Animator.StringToHash("SpellCastDelayAnimTrigger");
    protected override int AnimTriggerCast => 0;

    private bool CheckCanCast()
    {
        if (Targeting.GetTarget() == null) return Vector3.Distance(_targetPoint, transform.position) <= AreaInfo.Radius;
        else return false;
    }

    public bool IsSleepInnerDarknessTalentActive { get => _isSleepInnerDarknessTalentActive; set => _isSleepInnerDarknessTalentActive = value; }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        var multiMagic = Hero.CharacterState.GetState(States.MultiMagic) as MultiMagic;

        while (Targeting.GetTarget()?.Character == null && !_disactive)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget();
                //_target = GetRaycastTarget(true);
                //_runtimeTarget = Targeting.GetTarget();
                if (multiMagic != null) multiMagic.LastTarget = Targeting.GetTarget()?.Character;
            }
            yield return null;
        }

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Targeting.GetTarget()?.Character);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (Targeting.GetTarget()?.Character != null) CmdApplyAbsorptionState(Targeting.GetTarget()?.Character.gameObject);

        var multiMagic = Hero.CharacterState.GetState(States.MultiMagic) as MultiMagic;

        if (multiMagic != null)
        {
            foreach (var character in multiMagic.PopPendingTargets())
            {
                TryPayCost();
                CmdApplyAbsorptionState(character.gameObject);
            }
        }

        AfterCastJob();

        yield return null;
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
        //_target = null;
    }

    [Command]
    private void CmdApplyAbsorptionState(GameObject targetGameObject)
    {
        var targetCharacter = targetGameObject.GetComponent<Character>();
        if (targetCharacter != null)
        {
            targetCharacter.CharacterState.AddState(States.Sleep, duration, 0, _playerLinks.gameObject, name);
        }
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0) Targeting.SetTarget((ITargetable)(targetInfo.GetTargets()[0] as Character));
    }

    #region Talent

    public void SleepInnerDarknessTalent(bool value) => _isSleepInnerDarknessTalentActive = value;

    #endregion
}
