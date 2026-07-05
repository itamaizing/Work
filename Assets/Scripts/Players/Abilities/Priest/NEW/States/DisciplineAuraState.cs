using System.Collections.Generic;
using UnityEngine;

public class DisciplineAuraState : RefreshingState
{
    public override States State      => States.DisciplineAura;
    public override StateType Type { get; }
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => new();

    private const float _auraRadius   = 10f;
    private const float _auraDuration = 6f;
    private const int   _maxStacks    = 4;
    private const float _checkRate    = 0.5f;
    private const float _priestBonus  = 0.10f;
    private const float _allyBonus    = 0.05f;

    private readonly Dictionary<Character, List<AttributeModifier>> _modifiers = new();
    private readonly HashSet<Character> _charactersInRadius = new();

    private Character  _priest;
    private float      _checkTimer;

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
        characterState     = character;
        _priest            = personWhoMadeBuff;
        duration           = _auraDuration;
        MaxStacksCount     = _maxStacks;
        currentStacksCount = 0;
        _checkTimer        = 0f;

        AddModifierToCharacter(_priest, isPriest: true);
    }

    public override void UpdateState()
    {
        if (characterState == null || !characterState.isOwned) return;
        _checkTimer += Time.deltaTime;
        if (_checkTimer < _checkRate) return;
        _checkTimer = 0f;

        var allyLayer = LayerMask.GetMask("Allies");
        var hits      = Physics.OverlapSphere(_priest.transform.position, _auraRadius, allyLayer);

        var currentInRadius = new HashSet<Character>();
        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<Character>(out var ally)) continue;
            if (ally == _priest || ally.IsDead) continue;
            currentInRadius.Add(ally);
        }

        foreach (var ally in currentInRadius)
        {
            if (_charactersInRadius.Contains(ally)) continue;
            _charactersInRadius.Add(ally);
            for (int i = 0; i < currentStacksCount; i++)
                AddModifierToCharacter(ally, isPriest: false);
        }

        var toRemove = new List<Character>();
        foreach (var ally in _charactersInRadius)
            if (!currentInRadius.Contains(ally))
                toRemove.Add(ally);

        foreach (var ally in toRemove)
        {
            _charactersInRadius.Remove(ally);
            RemoveAllModifiersFromCharacter(ally);
        }
    }

    public override bool Stack(float time)
    {
        duration          = _auraDuration;
        RemainingDuration = _auraDuration;
        
        if (currentStacksCount < MaxStacksCount)
        {
            foreach (var ally in _charactersInRadius)
                AddModifierToCharacter(ally, isPriest: false);

            if (_priest != null)
                AddModifierToCharacter(_priest, isPriest: true);
        }

        return true;
    }

    protected override void ExitState()
    {
        foreach (var character in new List<Character>(_modifiers.Keys))
            RemoveAllModifiersFromCharacter(character);

        _modifiers.Clear();
        _charactersInRadius.Clear();
        currentStacksCount = 0;
        duration           = 0f;

        characterState?.RemoveStateFromList(this);
        characterState = null;
        _priest        = null;
    }

    private void AddModifierToCharacter(Character character, bool isPriest)
    {
        if (character == null || character.IsDead) return;
        float bonusPercent = isPriest ? _priestBonus : _allyBonus;
        float bonusValue   = character.Health.MaxValue * bonusPercent;

        character.Health.AddMax(bonusValue,true);

        var modifier = new AttributeModifier(bonusPercent, ModifierType.Percent);
        if (!_modifiers.ContainsKey(character))
            _modifiers[character] = new List<AttributeModifier>();
        _modifiers[character].Add(modifier);
    }

    private void RemoveAllModifiersFromCharacter(Character character)
    {
        if (!_modifiers.TryGetValue(character, out var modList)) return;

        float totalMaxBonus = 0f;
        foreach (var modifier in modList)
            totalMaxBonus += character.Health.MaxValue * modifier.Value;

        character.Health.AddMax(-totalMaxBonus,true);

        _modifiers.Remove(character);
    }
}