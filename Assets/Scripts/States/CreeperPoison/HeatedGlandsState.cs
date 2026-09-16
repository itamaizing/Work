using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class HeatedGlandsState : StateStackingRefreshing
{
    private int _maxStacks = 7;

    private float _baseDuration;

    private float _baseManaRegenIncrease = 0.3f;
    private float _allManaRegenIncrease;
    private float _baseManaRegen;

    private Resource _playerMana;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Strengthening };
    public override States State => States.HeatedGlands;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        Debug.Log("HeatedGlands / Apply");

        SetMaxStacks(_maxStacks);

        _playerMana = personWhoMadeBuff.TryGetResource(ResourceType.Mana);

        _baseDuration = durationToExit;

        _baseManaRegen = personWhoMadeBuff.TryGetResource(ResourceType.Mana).RegenerationValue;

        if (CurrentStacksCount < MaxStacksCount)
        {
            CurrentStacksCount++;
            IncreasingManaRegeneration();
        }
    }

    public override void UpdateState()
    {

    }

    public override void ExitState()
    {
        sourceCaster.TryGetResource(ResourceType.Mana).RegenerationValue = _baseManaRegen;
        
        _allManaRegenIncrease = 0;

        CurrentStacksCount = 0;

        characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (CurrentStacksCount < MaxStacksCount)
        {
            CurrentStacksCount++;

            RemainingDuration = _baseDuration;

            IncreasingManaRegeneration();

            return true;
        }
        else
        {
            RemainingDuration = _baseDuration;

            return true;
        }
    }

    [Server]
    private void IncreasingManaRegeneration()
    {
        _allManaRegenIncrease += _baseManaRegenIncrease;
        Debug.Log("HeatedGlands / IncreasingManaRegen / _allManaRegenIncrease = " + _allManaRegenIncrease);
        float increasingManaRegen = _baseManaRegen * _allManaRegenIncrease;
        Debug.Log("HeatedGlands / IncreasingManaRegen / increasingManaRegen = " + increasingManaRegen);
        sourceCaster.TryGetResource(ResourceType.Mana).RegenerationValue = increasingManaRegen;
        Debug.Log("HeatedGlands / IncreasingManaRegen / player current ManaRegen = " + sourceCaster.TryGetResource(ResourceType.Mana).RegenerationValue);
    }
}
