using Org.BouncyCastle.Crypto.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbsorptionOfPoisonsState : AbstractCharacterState
{
    private Character _player;

    private int _currentStack = 1;

    private float _maxHealth;
    private float _baseHealthIncrease = 0.1f;
    private float _increasedHealth;
    private float _allIncreasedHealth;

    private float _duration;
    private float _baseDuration;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Absorptions };
    public override float TEST_ChangeableValue { get; set; }

    public override States State => States.AbsorptionOfPoison;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        Debug.Log("AbsorptionState / EnterState");
        _characterState = character;
        _player = personWhoMadeBuff;

        _duration = durationToExit;
        _baseDuration = durationToExit;

        _maxHealth = _player.Health.MaxValue;

        IncreaseHealth();
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;
        if (_duration <= 0)
        {
            ExitState();
        }
    }

    public override bool Stack(float time)
    {
        _currentStack++;
        _duration = _baseDuration;
        IncreaseHealth();
        return true;
    }

    public override void ExitState()
    {
        _player.Health.ChangedMaxValue(-_allIncreasedHealth);
        Debug.Log("ExitState / playerMaxHealth = " + _player.Health.MaxValue);
        ResetValues();

        _characterState.RemoveState(this);
    }

    private void IncreaseHealth()
    {
        Debug.Log("IncreaseHealth");
        float increasingValue = _currentStack * _baseHealthIncrease;
        Debug.Log("IncreaseHealth / increasingValue = " + increasingValue);
        _increasedHealth = _maxHealth * increasingValue;
        Debug.Log("IncreaseHealth / _increasedHealth = " + _increasedHealth);
        _player.Health.ChangedMaxValue(_increasedHealth);
        Debug.Log("IncreaseHealth / playerMaxHealth = " + _player.Health.MaxValue);
        _allIncreasedHealth += _increasedHealth;
    }

    private void ResetValues()
    {
        _allIncreasedHealth = 0;
        _currentStack = 0;
        _duration = 0;
        _baseHealthIncrease = 0.1f;
        _increasedHealth = 0;
    }
}
