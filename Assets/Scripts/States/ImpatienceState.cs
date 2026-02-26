using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class ImpatienceState : AbstractCharacterState
{
    private float _durationRemaining;

    private static readonly HashSet<Character> ActiveCharacters = new();

    private bool _isProcessingSharedDamage;
    private bool _isAccumulationActive;
    private BasePsionicEnergy _casterPsionic;

    private List<StatusEffect> _effects = new() { StatusEffect.Ability };

    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State => States.Impatience;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        health = character.Character.Health;
        this.personWhoMadeBuff = personWhoMadeBuff;

        _durationRemaining = durationToExit;

        if (!character.isServer) return;

        ActiveCharacters.Add(character.Character);
        health.OnBeforeDamage += HandleBeforeDamage;

        if (personWhoMadeBuff != null)
        {
            _casterPsionic = personWhoMadeBuff.GetComponent<BasePsionicEnergy>();

            if (_casterPsionic != null) _casterPsionic.OnAccumulationPsionicChanged += HandleAccumulationChanged;
        }
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

            if (health != null) health.OnBeforeDamage -= HandleBeforeDamage;

            if (_casterPsionic != null) _casterPsionic.OnAccumulationPsionicChanged -= HandleAccumulationChanged;
        }

        characterState.RemoveState(this);
    }

    private void HandleAccumulationChanged(bool value) => _isAccumulationActive = value;

    private void HandleBeforeDamage(ref Damage damage, Skill skill)
    {
        if (_isProcessingSharedDamage) return;
        if (damage.Value <= 0) return;

        if (_isAccumulationActive && _casterPsionic != null)
        {
            float psiGain = damage.Value * 0.1f;
            _casterPsionic.AddAndResetDecay(psiGain);
        }

        List<Character> recipients = new List<Character>(ActiveCharacters);

        if (personWhoMadeBuff != null && !personWhoMadeBuff.IsDead)
        {
            if (!recipients.Contains(personWhoMadeBuff))
                recipients.Add(personWhoMadeBuff);
        }

        if (recipients.Count <= 1) return;

        float originalDamage = damage.Value;
        float dividedDamage = originalDamage / recipients.Count;

        _isProcessingSharedDamage = true;

        foreach (var character in recipients)
        {
            if (character == characterState.Character) continue;

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