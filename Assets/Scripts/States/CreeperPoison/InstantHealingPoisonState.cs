using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstantHealingPoisonState : AbstractCharacterState
{
    /* For PoisonBall Ability */

    private Character _player;
    private HealingPoisonPerSecondState _healingPoisonPerSecondState;

    private int _maxStacks = 1;

    private float _baseHealingValue = 14.0f;

    private float _totalHealed;

    private float _timeBetweenHeal;
    private float _startTimeBetweenHeal = 1.0f;

    private float _duration;
    private float _baseDuration;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Healing };

    public override float TEST_ChangeableValue { get => _baseHealingValue; set => _baseHealingValue = value; }
    public override States State => States.InstantHealingPoison;
    public override StateType Type => StateType.Physical;
    public override BuffDebuff BuffDebuff => BuffDebuff.Buff;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        Debug.Log("InstantHealingPoison / EnterState");

        MaxStacksCount = _maxStacks;

        _characterState = character;

        _duration = durationToExit;
        _baseDuration = durationToExit;
        _player = personWhoMadeBuff;
    }

    public override void UpdateState()
    {
        MakeHeal();
    }

    public override void ExitState()
    {
        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        return false;
    }

    [Server]
    private void MakeHeal()
    {
        if (_characterState.CheckForState(States.HealingPoisonPerSecond))
        {
            _healingPoisonPerSecondState = (HealingPoisonPerSecondState)_characterState.GetState(States.HealingPoisonPerSecond);
            float multiplierHealValue = _healingPoisonPerSecondState.TotalHealValue;
            Debug.Log("IntstantHealing / MakeHeal / if / multiplierHealValue = " + multiplierHealValue);
            _totalHealed = _baseHealingValue + multiplierHealValue;
            Debug.Log("IntstantHealing / MakeHeal / if / baseHeal = 14f / _totalHealed = " + _totalHealed);
        }
        else
        {
            _totalHealed = _baseHealingValue;
            Debug.Log("IntstantHealing / MakeHeal / else / baseHeal = 14f / _totalHealed = " + _totalHealed);
        }

        Heal heal = new Heal
        {
            Value = _totalHealed,
            DamageableSkill = null,
        };

        _characterState.Character.Health.Heal(ref heal, null);

        ExitState();
    }

}
