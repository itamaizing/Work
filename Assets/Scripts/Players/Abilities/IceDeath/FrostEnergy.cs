using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class FrostEnergy : Skill
{
    [SerializeField] private float _runeCost = 1f;

    private Coroutine _drainRoutine;

    private const float StartDelay = 2f;
    private const float DrainInterval = 0.1f;
    private const float EnergyPerTick = 1f;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => true;

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Hero);
        callbackDataSaved(targetInfo);
        yield break;
    }

    protected override IEnumerator CastJob()
    {
        if (Hero == null || Hero.CharacterState == null)
            yield break;

        if (!Cost.TryPaySingle(_runeCost, ResourceType.Rune, shouldModify: false))
        {
            TryCancel(true);
            yield break;
        }

        CmdSkillToggleFrostEnergyState(Hero.gameObject);
        yield break;
    }

    [Command]
    private void CmdSkillToggleFrostEnergyState(GameObject targetObj)
    {
        if (targetObj == null) return;

        Character character = targetObj.GetComponent<Character>();
        if (character == null || character.CharacterState == null) return;

        if (character.CharacterState.CheckForState(States.FrostEnergy))
        {
            character.CharacterState.RemoveState(States.FrostEnergy);
            StopDrain(character);
        }
        else
        {
            character.CharacterState.AddState( States.FrostEnergy, 999f, 0f, character.gameObject, name);
            StartDrain(character);
        }
    }

    [Server]
    private void StartDrain(Character character)
    {
        if (_drainRoutine != null)
            StopCoroutine(_drainRoutine);

        _drainRoutine = StartCoroutine(DrainRoutine(character));
    }

    [Server]
    private void StopDrain(Character character)
    {
        if (_drainRoutine != null)
        {
            StopCoroutine(_drainRoutine);
            _drainRoutine = null;
        }
    }

    [Server]
    private IEnumerator DrainRoutine(Character character)
    {
        yield return new WaitForSeconds(StartDelay);

        while (character != null && character.CharacterState.CheckForState(States.FrostEnergy))
        {
            if (!Cost.TryPaySingle(EnergyPerTick, ResourceType.Energy, shouldModify: true))
            {
                character.CharacterState.RemoveState(States.FrostEnergy);
                break;
            }

            yield return new WaitForSeconds(DrainInterval);
        }

        _drainRoutine = null;
    }
}