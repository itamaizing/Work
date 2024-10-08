using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SparkTalentHealthState : AbstractCharacterState
{
    private float _healthBuffActiveTime = 2f;
    private float _healthBoostPercentage = 0.25f;

    private List<StatusEffect> _effects = new ();

    public override States State => States.SpiritHealth;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _characterState = character;
        _healthBuffActiveTime = durationToExit;
        _healthBoostPercentage = damageToExit;
        ApplyBuff();
    }

    public override void UpdateState()
    {
        _healthBuffActiveTime -= Time.deltaTime;

        if (_healthBuffActiveTime <= 0)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        RemoveBuff();
        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        return false;
    }

    private void ApplyBuff()
    {
        var heal = _characterState.Character.Health.CurrentValue * _healthBoostPercentage;
        _characterState.Character.Health.Add(heal);
    }

    private void RemoveBuff()
    {
        var heal = -_characterState.Character.Health.CurrentValue * _healthBoostPercentage;
        _characterState.Character.Health.Add(heal);
    }
}