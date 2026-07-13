using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class SleepSpell : Skill
{
    [SerializeField] private Character _playerLinks;
    [SerializeField] private float _heroDuration = 6f;
    [SerializeField] private float _creatureDuration = 40f;
    
    //private Character _target;
    //private Character _runtimeTarget;
    private bool _isSleepInnerDarknessTalentActive = false;

    protected override bool IsCanCast => Targeting.GetTarget() != null && Vector3.Distance(Targeting.GetTarget().Transform.position, transform.position) <= AreaInfo.Radius;
    protected override int AnimTriggerCastDelay => Animator.StringToHash("SpellCastDelayAnimTrigger");
    protected override int AnimTriggerCast => 0;

    public bool IsSleepInnerDarknessTalentActive { get => _isSleepInnerDarknessTalentActive; set => _isSleepInnerDarknessTalentActive = value; }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        var multiMagic = Hero.CharacterState.GetState(States.MultiMagic) as MultiMagic;

        TargetInfo targetInfo = new TargetInfo();

        while (Targeting.GetTempTarget()?.Targetable == null && !_disactive)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget(Targeting.GetMousePoint(), 0.5f);

                var temp = Targeting.GetTempTarget()?.Targetable as Character;

                if (temp != null)
                {
                    if (multiMagic != null)
                        multiMagic.LastTarget = temp;

                    break;
                }
            }

            yield return null;
        }

        var target = Targeting.GetTempTarget()?.Targetable;

        if (target != null)
        {
            targetInfo.AddTarget(target);
            callbackDataSaved(targetInfo);
        }
    }

    protected override IEnumerator CastJob()
    {
        var target = Targeting.GetTarget()?.Character;

        if (target == null)
            yield break;

        CmdApplyAbsorptionState(target.gameObject);

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
            targetCharacter.CharacterState.AddState(States.Sleep, targetCharacter is HeroComponent ? _heroDuration : _creatureDuration, 0, _playerLinks.gameObject, name);
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
