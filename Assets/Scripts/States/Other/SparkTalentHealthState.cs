using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SparkTalentHealthState : AbstractCharacterState
{
    private Skill _skill;
    private float _healthBuffActiveTime = 2f;
    private float _healthBoostPercentage = 0.25f;

    private List<StatusEffect> _effects = new ();
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State => States.SparkTalentHealthBuff;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _skill = personWhoMadeBuff.Abilities.Abilities.FirstOrDefault(o => o.Name == skillName);
        
        characterState = character;
        _healthBuffActiveTime = durationToExit;
        _healthBoostPercentage = damageToExit;
        ApplyBuff();
    }

    public override void UpdateState()
    {
        _healthBuffActiveTime -= Time.deltaTime;

        if (_healthBuffActiveTime <= 0)
        {
            GlobalExit();
        }
    }

    protected override void ExitState()
    {
        RemoveBuff();
        characterState.RemoveStateFromList(this);
    }

    /*public override bool Stack(float time)
    {
        return false;
    }*/

    private void ApplyBuff()
    {
        var healValue = characterState.Character.Health.CurrentValue * _healthBoostPercentage;
        var heal = new Heal { Value = healValue };
        
        _skill.ApplyHeal(heal, characterState.gameObject, _skill, nameof(States.SpiritEnergy));  
    }

    private void RemoveBuff()
    {
        var healValue = characterState.Character.Health.CurrentValue * _healthBoostPercentage;
        var heal = new Heal { Value = -healValue };
        
        _skill.ApplyHeal(heal, characterState.gameObject, _skill, nameof(States.SpiritEnergy));  
    }
}