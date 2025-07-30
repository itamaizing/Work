using System.Collections.Generic;
using UnityEngine;

public class Calmness : AbstractCharacterState
{
    private const float _manaRegenPercent = 10;
    private const int _baseMaxStacks = 2;
    private int _lastTreesCount;
    private float _duration;
    private float _regenAmount;

    private Resource manaResource;

    private List<StatusEffect> _effects = new List<StatusEffect> { StatusEffect.Healing };
    public override States State => States.Calmness;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _characterState = character;
        _health = character.Character.Health;
        manaResource = character.Character.TryGetResource(ResourceType.Mana);
        _personWhoMadeBuff = personWhoMadeBuff;
        MaxStacksCount = _baseMaxStacks;

        _duration = durationToExit;

        RecalcRegenAmount();
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;
        if (_duration <= 0)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        CurrentStacksCount = 0;
        _characterState.StateIcons.RemoveItemByState(State);
        _characterState.RemoveState(this);
    }

    public override bool Stack(float newDuration)
    {
        _duration = Mathf.Max(_duration, newDuration);
        Debug.Log(MaxStacksCount);

        if (CurrentStacksCount < MaxStacksCount) CurrentStacksCount++;

        RecalcRegenAmount();

        return true;
    }

    public void UpdateTreesCount(int newTreesCount)
    {
        _lastTreesCount = newTreesCount;
        MaxStacksCount = _baseMaxStacks + _lastTreesCount;

        if (CurrentStacksCount > MaxStacksCount) CurrentStacksCount = MaxStacksCount;

        RecalcRegenAmount();
    }

    public void ApplyRegen()
    {
        if (manaResource != null && _regenAmount > 0) manaResource.CmdAdd(_regenAmount);
    }

    private void RecalcRegenAmount()
    {
        if (manaResource != null) _regenAmount = manaResource.MaxValue * _manaRegenPercent * CurrentStacksCount;
    }

}
