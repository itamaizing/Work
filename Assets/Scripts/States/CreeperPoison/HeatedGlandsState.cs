using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class HeatedGlandsState : StackableState
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

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        Debug.Log("HeatedGlands / EnterState");

        MaxStacksCount = _maxStacks;

        _playerMana = personWhoMadeBuff.TryGetResource(ResourceType.Mana);

        _baseDuration = durationToExit;

        _baseManaRegen = personWhoMadeBuff.TryGetResource(ResourceType.Mana).RegenerationValue;

        if (currentStacksCount < MaxStacksCount)
        {
            currentStacksCount++;
            IncreasingManaRegeneration();
        }
    }

    public override void UpdateState()
    {

    }

    protected override void ExitState()
    {
        personWhoMadeBuff.TryGetResource(ResourceType.Mana).RegenerationValue = _baseManaRegen;
        
        _allManaRegenIncrease = 0;

        currentStacksCount = 0;

        characterState.RemoveStateFromList(this);
    }

    public override bool Stack(float time)
    {
        if (currentStacksCount < MaxStacksCount)
        {
            currentStacksCount++;

            duration = _baseDuration;

            IncreasingManaRegeneration();

            return true;
        }
        else
        {
            duration = _baseDuration;

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
        personWhoMadeBuff.TryGetResource(ResourceType.Mana).RegenerationValue = increasingManaRegen;
        Debug.Log("HeatedGlands / IncreasingManaRegen / player current ManaRegen = " + personWhoMadeBuff.TryGetResource(ResourceType.Mana).RegenerationValue);
    }
}
