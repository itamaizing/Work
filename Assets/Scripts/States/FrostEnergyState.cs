using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrostEnergyState : RefreshingState
{
    public override States State => States.FrostEnergy;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => new List<StatusEffect>
    {
        StatusEffect.Freezing
    };

    public override Schools Schools => Schools.Water;

    private Coroutine _drainRoutine;
    private Character _owner;

    private const float StartDelay = 2f;
    private const float DrainInterval = 0.1f;
    private const float EnergyPerTick = 1f;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        _owner = character.Character;

        if (_owner.isServer) _drainRoutine = _owner.StartCoroutine(DrainEnergyRoutine());
    }

    public override void UpdateState()
    {

    }

    public override void ExitState()
    {
        if (_drainRoutine != null && _owner != null)
        {
            _owner.StopCoroutine(_drainRoutine);
            _drainRoutine = null;
        }

        base.ExitState();
    }

    private IEnumerator DrainEnergyRoutine()
    {
        yield return new WaitForSeconds(StartDelay);

        if (_owner == null) yield break;

        if (!_owner.TryGetResource(ResourceType.Energy, out var resource))
            yield break;

        Energy energy = resource as Energy;

        while (characterState.CheckForState(States.FrostEnergy))
        {
            if (energy.CurrentValue <= 0)
            {
                characterState.RemoveState(this);
                yield break;
            }

            energy.CmdUse(EnergyPerTick);

            yield return new WaitForSeconds(DrainInterval);
        }
    }
}