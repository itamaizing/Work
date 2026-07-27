using System.Collections.Generic;
using UnityEngine;

public class VampirismAura : AuraStateHandler
{
    protected override void OnTargetEnter(Character target)
    {
        ApplyBuff(target);
    }

    protected override void OnTargetExit(Character target)
    {
        RemoveBuff(target);
    }

    protected override void OnAuraDisabled()
    {
        if (_owner != null)
            RemoveBuff(_owner);

        RemoveEffectsFromAllTargets();
    }

    private void ApplyBuff(Character target)
    {
        if (isClient)
        {
            CmdApplyStateToTarget(target.gameObject, States.VampirismBuff, float.PositiveInfinity,
                Schools.Physical, _owner.gameObject, nameof(VampirismAura), 0);
        }
    }

    private void RemoveBuff(Character target)
    {
        if(isClient)
            CmdRemoveStateFromTarget(target.gameObject, States.VampirismBuff);
        if(isServer)
            target.GetComponent<CharacterState>().RemoveState(States.VampirismBuff);
    }
}
