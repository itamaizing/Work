using System.Collections.Generic;
using UnityEngine;

public class DefenceReductionState : AbstractCharacterState
{
    private float _healthBoostPercentage = 0.25f;
    private float _defaultPhysDef = 0;

    private List<StatusEffect> _effects = new ();
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State => States.DefenseReduction;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        duration = durationToExit;
        _healthBoostPercentage = damageToExit;
        ApplyBuff();
    }

    public override void UpdateState()
    {

    }

    protected override void ExitState()
    {
        RemoveBuff();
    }

    /*public override bool Stack(float time)
    {
        return false;
    }*/

    private void ApplyBuff()
    {
        _defaultPhysDef = characterState.Character.Health.DefPhysDamage;
        characterState.Character.Health.SetPhysicDef(_defaultPhysDef * _healthBoostPercentage);
    } 

    private void RemoveBuff()
    {
        characterState.Character.Health.SetPhysicDef(_defaultPhysDef);
    }
}