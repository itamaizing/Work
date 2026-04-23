using UnityEngine;

public class AirElement : AuraStateHandler
{
    [SerializeField] private float _buffDuration = -1f;

    protected override void OnTargetEnter(Character target)
    {
        CmdApplyStateToTarget(target.gameObject, States.PushingWindAura, _buffDuration, Schools.Air, _owner.gameObject, nameof(AirElement));
    }

    protected override void OnTargetExit(Character target)
    {
        CmdRemoveStateFromTarget(target.gameObject, States.PushingWindAura);
    }

    protected override void OnAuraDisabled()
    {
        RemoveEffectsFromAllTargets();
    }
}
