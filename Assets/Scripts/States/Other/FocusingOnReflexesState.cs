using System.Collections.Generic;
using UnityEngine;

public class FocusingOnReflexesState : AbstractCharacterState
{
    private float _duration;
    private float _originalEvadeMelee;
    private float _originalEvadeRange;

    private Character _character;

    private List<StatusEffect> _effects = new() { StatusEffect.Evade };

    public override States State => States.FocusingOnReflexesState;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    protected override void OnEnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        _duration = durationToExit;
        _character = character.Character;

        var health = _character.Health;

        _originalEvadeMelee = health.EvadeMeleeDamage;
        _originalEvadeRange = health.EvadeRangeDamage;

        health.EvadeMeleeDamage = 60f;
        health.EvadeRangeDamage = 100f;

        health.Evaded += OnEvaded;
    }

    public override void OnUpdateState()
    {
    }

   /* public override bool Stack(float time)
    {
        _duration = Mathf.Max(_duration, time);
        return true;
    }*/

    protected override void OnExitState()
    {
        if (_character != null)
        {
            var health = _character.Health;

            health.EvadeMeleeDamage = _originalEvadeMelee;
            health.EvadeRangeDamage = _originalEvadeRange;
            health.Evaded -= OnEvaded;
        }
    }

    private void OnEvaded()
    {
        ExitState();
        Debug.Log("Exit for FocusingOnReflexesState");
    }
}
