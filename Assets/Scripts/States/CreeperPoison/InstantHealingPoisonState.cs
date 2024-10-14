using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstantHealingPoisonState : AbstractCharacterState
{
    //private List<Talent> _talents = new();
    //private SurgeTreatment _surgeTreatment;
    public bool turnOff = false;

    private Character _player;

    private int _currentStacks = 0;
    private int _maxStacks = 1;

    private float _baseHealingValue = 14.0f;
    private float _healingValuePerSecond;

    private float _totalHealed = 0.0f;

    private float _timeBetweenHeal;
    private float _startTimeBetweenHeal = 1.0f;

    private float _duration;
    private float _baseDuration;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Healing };

    public override float TEST_ChangeableValue { get => _baseHealingValue; set => _baseHealingValue = value; }
    public override States State => States.InstantHealingPoison;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        Debug.Log("InstantHealingPoison / EnterState");

        _characterState = character;

        _duration = durationToExit;
        _baseDuration = durationToExit;
        _player = personWhoMadeBuff;

        //Debug.Log("_player == " + _player);

        if (_currentStacks < _maxStacks)
        {
            AddStacks();
        }
        //Debug.Log($"SetPlayer in EnterHealingPoisonState == {_player}");

        //if (_player != null)
        //{
        //	_talents = _player.CharacterState.Character.TalentSystem.Talents;
        //	Debug.Log("HealingPoison player == " + _player);

        //	foreach (Talent talent in _talents)
        //	{
        //		Debug.Log("Checking talents: " + talent.name + ", Type: " + talent.GetType());
        //		if (talent is SurgeTreatment surgeTreatment)
        //		{
        //			Debug.Log("if / talents");
        //			if (_surgeTreatment == null)
        //			{
        //				_surgeTreatment = surgeTreatment;
        //				Debug.Log("SurgeTreatment == " + _surgeTreatment);
        //			}
        //		}
        //	}
        //}
    }

    public override void UpdateState()
    {
        _timeBetweenHeal -= Time.deltaTime;
        if (_timeBetweenHeal <= 0)
        {
            MakeHeal();
            _timeBetweenHeal = _startTimeBetweenHeal;
        }

        _duration -= Time.deltaTime;
        if (_duration < 0 || turnOff)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        return false;
    }

    public void AddStacks()
    {
        Debug.Log("InstantHealingPoison / AddStacks");
        _currentStacks++;
        _duration = _baseDuration;
    }

    private void MakeHeal()
    {
        _characterState.Character.Health.Heal(_baseHealingValue);
        //if (_surgeTreatment != null && _surgeTreatment.IsActive)
        //{
        //	_totalHealed += _baseHealingValue;
        //	Debug.Log("TotalHeal == " + _totalHealed);
        //}
    }

    //private void InstantHeal()
    //{
    //      Debug.Log("Instant Heal Method");
    //     _characterState.Health.AddHeal(_totalHealed);
    //      _totalHealed = 0.0f;
    //}
}
