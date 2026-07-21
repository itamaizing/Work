using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class BleedingSpell : Skill, IMultiMagicSkill
{
    [SerializeField] private Character _playerLinks;
    [SerializeField] private float duration;
    [SerializeField] private float _baseDamage = 10;

    //private Character _target;
    private Vector3 _targetPoint = Vector3.positiveInfinity;

    protected override bool IsCanCast => Targeting.GetTarget()?.Character != null && Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position) <= AreaInfo.Radius;
    protected override int AnimTriggerCastDelay => Animator.StringToHash("SpellCastDelayAnimTrigger");
    protected override int AnimTriggerCast => 0;

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        Damage = duration * _baseDamage;

        var multiMagic = Hero.CharacterState.GetState(States.MultiMagic) as MultiMagic;

        while (Targeting.GetTarget()?.Character == null && !_disactive)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget();
                //_target = GetRaycastTarget(true);
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
        Damage = _baseDamage;
        if (Targeting.GetTarget()?.Character != null) CmdApplyAbsorptionState(Targeting.GetTarget()?.Character.gameObject);
        
        AfterCastJob();

        yield return null;
    }

    protected override void ClearData()
    {
        _targetPoint = Vector3.positiveInfinity;
        Targeting.ClearTarget();
        //_target = null;
    }

    [Command]
    private void CmdApplyAbsorptionState(GameObject targetGameObject)
    {
        var targetCharacter = targetGameObject.GetComponent<Character>();
        if (targetCharacter != null)
        {
            targetCharacter.CharacterState.AddState(States.Bleeding, duration, Damage, _playerLinks.gameObject, name);
        }
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0) Targeting.SetTarget((ITargetable)(targetInfo.GetTargets()[0] as Character));
    }

    public void HandleExtraTarget(Character target)
    {
        TryPayCost();
        CmdApplyAbsorptionState(target.gameObject);
    }
}

