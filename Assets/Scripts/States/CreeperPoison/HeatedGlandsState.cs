using System.Collections.Generic;
using UnityEngine;

public class HeatedGlandsState : AbstractCharacterState
{
    private int _maxStacks = 10;

    private float _duration;
    private float _baseDuration;
    private float _amountManaIncreasingValue = 0.02f;
    private float _newMaxManaPlayer;
    private float _maxManaPlayer;

    private Character _player;
    private Resource _playerMana;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Strengthening };
    public override float TEST_ChangeableValue { get; set; }
    public override States State => States.HeatedGlands;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        MaxStacksCount = _maxStacks;

        _characterState = character;
        _player = personWhoMadeBuff;
        _playerMana = _player.TryGetResource(ResourceType.Mana);

        _duration = durationToExit;
        _baseDuration = _duration;

        _maxManaPlayer = _playerMana.MaxValue;

        if (CurrentStacksCount < MaxStacksCount)
        {
            AddStack();
        }

    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;
        if (_duration < 0)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        _playerMana.ChangedMaxValue(-_newMaxManaPlayer);

        CurrentStacksCount = 0;
        _newMaxManaPlayer = 0;

        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (CurrentStacksCount < MaxStacksCount)
        {
            AddStack();
            return true;
        }
        else
        {
            _duration = _baseDuration;
            return true;
        }
    }

    private void AddStack()
    {
        CurrentStacksCount++;
        _duration = _baseDuration;
        IncreasingAmountManaValue();
    }

    private void IncreasingAmountManaValue()
    {
        Debug.Log("HeatedGlands / IncreasingAmountManaValue");

        float bonusAmountMana = _amountManaIncreasingValue * _maxManaPlayer;
        Debug.Log("IncreasingMana / bonusMana == " + bonusAmountMana);

        _playerMana.ChangedMaxValue(bonusAmountMana);

        _newMaxManaPlayer += bonusAmountMana;
        Debug.Log("IncreasingMana / newMaxManaPlayer == " + _newMaxManaPlayer);

        Debug.Log("IncreasingMana / MaxManaPlayer after +bonusMana == " + _playerMana.MaxValue);
    }
}
