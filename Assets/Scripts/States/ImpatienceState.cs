using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class ImpatienceState : AbstractCharacterState
{
    private float _durationRemaining;

    private static readonly HashSet<Character> ActiveCharacters = new();

    private bool _isProcessingSharedDamage;

    private List<StatusEffect> _effects = new() { StatusEffect.Ability };

    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State => States.Impatience;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character,
        float durationToExit,
        float damageToExit,
        Character personWhoMadeBuff,
        string skillName)
    {
        characterState = character;
        health = character.Character.Health;
        this.personWhoMadeBuff = personWhoMadeBuff;

        _durationRemaining = durationToExit;

        if (!character.isServer) return;

        ActiveCharacters.Add(character.Character);
        health.OnBeforeDamage += HandleBeforeDamage;
    }

    public override void UpdateState()
    {
        _durationRemaining -= Time.deltaTime;

        if (_durationRemaining <= 0)
            ExitState();
    }

    public override void ExitState()
    {
        if (characterState.Character.isServer)
        {
            ActiveCharacters.Remove(characterState.Character);

            if (health != null)
                health.OnBeforeDamage -= HandleBeforeDamage;
        }

        characterState.RemoveState(this);
    }

    private void HandleBeforeDamage(ref Damage damage, Skill skill)
    {
        if (_isProcessingSharedDamage) return;
        if (damage.Value <= 0) return;
        if (ActiveCharacters.Count <= 1) return;

        float originalDamage = damage.Value;
        float dividedDamage = originalDamage / ActiveCharacters.Count;

        _isProcessingSharedDamage = true;

        foreach (var character in ActiveCharacters)
        {
            if (character == characterState.Character)
                continue;

            Damage sharedDamage = new Damage
            {
                Value = dividedDamage,
                Type = damage.Type,
                School = damage.School,
                Form = damage.Form,
                PhysicAttackType = damage.PhysicAttackType
            };

            character.Health.TryTakeDamage(ref sharedDamage, skill);
        }

        damage.Value = dividedDamage;

        _isProcessingSharedDamage = false;
    }
}