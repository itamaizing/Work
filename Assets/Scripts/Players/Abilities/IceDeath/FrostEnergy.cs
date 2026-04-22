using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class FrostEnergy : Skill
{
    [SerializeField] private float _runeCost = 1f;

    private Coroutine _drainRoutine;
    private RuneComponent _rune;

    private const float StartDelay = 2f;
    private const float DrainInterval = 0.1f;
    private const float EnergyPerTick = 1f;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => true;

    #region Talent

    private bool _isUseRuneBonusEffect;

    public void _UseRuneBonusEffect(bool value) => _isUseRuneBonusEffect = value;
    #endregion

    private void OnEnable()
    {
        if (Hero.TryGetResource(ResourceType.Rune, out var resource))
        {
            _rune = resource as RuneComponent;
            if (_rune != null) _rune.OnRuneSpent += HandleRuneSpent;
        }
    }

    private void OnDestroy()
    {
        if (_rune != null) _rune.OnRuneSpent -= HandleRuneSpent;
    }

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

        SkillToggleFrostEnergyState(Hero.gameObject);
        yield break;
    }

    private void SkillToggleFrostEnergyState(GameObject targetObj)
    {
        if (targetObj == null) return;

        Character character = targetObj.GetComponent<Character>();
        if (character == null || character.CharacterState == null) return;

        if (character.CharacterState.CheckForState(States.FrostEnergy))
        {
            character.CharacterState.CmdRemoveState(States.FrostEnergy);
            StopDrain(character);
        }
        else
        {
            character.CharacterState.CmdAddState( States.FrostEnergy, 999f, 0f, character.gameObject, name);
            StartDrain(character);
        }
    }

    private void HandleRuneSpent(float amount, Skill skill)
    {
        if (!_isUseRuneBonusEffect) return;

        if (!Hero.CharacterState.CheckForState(States.FrostEnergy)) return;

        ApplyEnergyBonusEffect(amount);
    }

    private void StartDrain(Character character)
    {
        if (_drainRoutine != null)
            StopCoroutine(_drainRoutine);

        _drainRoutine = StartCoroutine(DrainRoutine(character));
    }

    private void StopDrain(Character character)
    {
        if (_drainRoutine != null)
        {
            StopCoroutine(_drainRoutine);
            _drainRoutine = null;
        }
    }

    private IEnumerator DrainRoutine(Character character)
    {
        yield return new WaitForSeconds(StartDelay);

        while (character != null && character.CharacterState.CheckForState(States.FrostEnergy))
        {
            if (!Cost.TryPaySingle(EnergyPerTick, ResourceType.Energy, shouldModify: true))
            {
                character.CharacterState.CmdRemoveState(States.FrostEnergy);
                break;
            }

            yield return new WaitForSeconds(DrainInterval);
        }

        _drainRoutine = null;
    }

    private void ApplyEnergyBonusEffect(float spentRune)
    {
        if (_rune == null) return;

        //float bonusRune = spentRune * 2f;
        float bonusEnergy = spentRune * 0.4f;

        //_rune.CmdAdd(bonusRune);

        if (Hero.TryGetResource(ResourceType.Energy, out var resource))
        {
            Energy energy = resource as Energy;
            energy?.CmdAdd(bonusEnergy);
            energy?.ForceRegenNow();
        }
    }
}