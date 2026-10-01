using UnityEngine;

public class SwarmSpeedAuraStateHandler : AuraStateHandler
{
    [SerializeField] private SwarmCapacity _swarmCapacity;

    private const float AuraRadius = 10f;

    [SerializeField] private float _buffDuration = -1f;

    protected override float GetCurrentRadius() => AuraRadius;

    protected override void OnTargetEnter(Character target)
    {
        if (_swarmCapacity == null || target == null || _owner == null) return;

        float occupiedCapacity = _swarmCapacity.CurrentCounter;

        CmdApplyStateToTarget(
            target.gameObject,
            States.SwarmSpeed,
            _buffDuration,
            Schools.None,
            _owner.gameObject,
            nameof(SwarmSpeedAuraStateHandler),
            occupiedCapacity);
    }

    protected override void OnTargetExit(Character target)
    {
        if (target == null) return;

        CmdRemoveStateFromTarget(target.gameObject, States.SwarmSpeed);
    }
}