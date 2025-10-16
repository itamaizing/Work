using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class SleepSpell : Skill
{
    [SerializeField] private Character _playerLinks;
    [SerializeField] private float duration;

    private Character _target;
    private Character _runtimeTarget;
    private Vector3 _targetPoint = Vector3.positiveInfinity;
    private bool _isSleepInnerDarknessTalentActive = false;

    protected override bool IsCanCast => IsHaveCharge && _target != null && Vector3.Distance(_target.transform.position, transform.position) <= Radius;
    protected override int AnimTriggerCastDelay => Animator.StringToHash("SpellCastDelayAnimTrigger");
    protected override int AnimTriggerCast => 0;

    public bool IsSleepInnerDarknessTalentActive { get => _isSleepInnerDarknessTalentActive; set => _isSleepInnerDarknessTalentActive = value; }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        var multiMagic = Hero.CharacterState.GetState(States.MultiMagic) as MultiMagic;

        while (_target == null && !_disactive)
        {
            if (GetMouseButton)
            {
                //_target = GetRaycastTarget(true);
                _runtimeTarget = _target;
                if (multiMagic != null) multiMagic.LastTarget = _target;
            }
            yield return null;
        }

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Targets.Add(_runtimeTarget);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (_target != null) CmdApplyAbsorptionState(_target.gameObject);

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
        _target = null;
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
        if (targetInfo.Targets.Count > 0) _target = targetInfo.Targets[0] as Character;
    }

    #region Talent

    public void SleepInnerDarknessTalent(bool value) => _isSleepInnerDarknessTalentActive = value;

    #endregion
}
