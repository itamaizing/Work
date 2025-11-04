using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class BleedingSpell : Skill
{
    [SerializeField] private Character _playerLinks;
    [SerializeField] private float duration;
    [SerializeField] private float _baseDamage = 10;

    private Character _target;
    private Vector3 _targetPoint = Vector3.positiveInfinity;

    protected override bool IsCanCast => _target != null && Vector3.Distance(_target.transform.position, transform.position) <= Radius;
    protected override int AnimTriggerCastDelay => Animator.StringToHash("SpellCastDelayAnimTrigger");
    protected override int AnimTriggerCast => 0;

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        Damage = duration * _baseDamage;

        var multiMagic = Hero.CharacterState.GetState(States.MultiMagic) as MultiMagic;

        while (_target == null && !_disactive)
        {
            if (GetMouseButton)
            {
                //_target = GetRaycastTarget(true);
                if (multiMagic != null) multiMagic.LastTarget = _target;
            }
            yield return null;
        }

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Targets.Add(_target);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        Damage = _baseDamage;
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
        _targetPoint = Vector3.positiveInfinity;
        _target = null;
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
        if (targetInfo.Targets.Count > 0) _target = targetInfo.Targets[0] as Character;
    }
}

