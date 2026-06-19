using System.Collections.Generic;
using UnityEngine;

public class VampirismBuffState : RefreshingState
{
    private const float ManaRestorePercent   = 0.20f;
    private float _accumulatedDamageForRune = 0f;
    private float _energyVampiricMultiplier = 2f;
    private const float DamagePerRune = 100f;
    public override States State => States.VampirismBuff;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;
    private readonly List<StatusEffect> _effects = new();

    private NinjaResources _ninjaResources;

    public override void EnterState(CharacterState character, float durationToExit,
        float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        duration = durationToExit;
        
        if(_ninjaResources == null)
            if (characterState.Character.TryGetComponent<NinjaResources>(out NinjaResources resources)) _ninjaResources = resources;

        characterState.Character.DamageTracker.OnDamageTracked += OnDamageDealt;
    }

    public override void ExitState()
    {
        if (characterState != null)
            characterState.Character.DamageTracker.OnDamageTracked -= OnDamageDealt;

        _accumulatedDamageForRune = 0;
        
        characterState.RemoveState(this);
    }

    public override void UpdateState() { }

    public override bool Stack(float time) => false;

    private void OnDamageDealt(Damage damage, GameObject target)
    {
        if (damage.Value <= 0) return;

        if (_ninjaResources != null)
        {
            if (characterState.Character.TryGetResource(ResourceType.Energy) is Energy energy)
            {
                float energyToRestore = damage.Value * ManaRestorePercent;
                if (characterState.CheckForState(States.HardenedFlesh))
                {
                    energyToRestore *= _energyVampiricMultiplier;
                }

                energy.Add(energyToRestore);
            }

            _accumulatedDamageForRune += damage.Value;

            while (_accumulatedDamageForRune >= DamagePerRune)
            {
                _accumulatedDamageForRune -= DamagePerRune;

                if (characterState.Character.TryGetResource(ResourceType.Rune) is RuneComponent rune)
                {
                    rune.Add(1);
                }
            }
        }
        else
        {
            if (characterState.Character.TryGetResource(ResourceType.Mana) is Mana mana)
            {
                mana.Add(damage.Value * ManaRestorePercent);
            }
        }
    }
}