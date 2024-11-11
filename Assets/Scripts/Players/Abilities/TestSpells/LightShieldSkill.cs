using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightShieldSkill : Skill
{
    [SerializeField] private Character _playerLinks;
    private Character _target;

    protected override bool IsCanCast => IsHaveCharge && _target != null;

    protected override IEnumerator PrepareJob()
    {
        while (_target == null)
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
            targetCharacter.CharacterState.AddState(States.LightShield, 20, 100, _playerLinks.gameObject, name);
        }
    }
}
