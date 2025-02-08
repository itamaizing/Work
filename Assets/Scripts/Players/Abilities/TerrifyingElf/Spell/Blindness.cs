using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blindness : Skill
{
    [SerializeField] private Character _playerLinks;
    [SerializeField] private float duration;
    private Character _target;

    protected override bool IsCanCast => IsHaveCharge && _target != null;

    protected override int AnimTriggerCastDelay => Animator.StringToHash("SpellCastDelayAnimTrigger");

    protected override int AnimTriggerCast => 0;

    protected override IEnumerator PrepareJob()
    {
        while (_target == null && !_disactive)
        {
            if (GetMouseButton)
            {
                _target = GetRaycastTarget(true);
            }
            yield return null;
        }
    }

    protected override IEnumerator CastJob()
    {
        if (_target != null)
        {
            CmdApplyAbsorptionState(_target.gameObject);
            TryUseCharge();
        }
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
            targetCharacter.CharacterState.AddState(States.Blind, duration, 0, _playerLinks.gameObject, name);
        }
    }
}
