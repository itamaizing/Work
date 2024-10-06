using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WitheringPoisonState : AbstractCharacterState
{
    public bool turnOff = false;

    private List<Skill> _skills = new();
    private List<Talent> _talents = new();
    private BindingPoison _bindingPoison;
    private Character _player;

    private int _currentStacks = 0;
    private int _maxStacks = 3;

    private float _timeBetweenTakeAwayMana;
    private float _startTimeBetweenTakeAwayMana = 1f;

    private float _duration;
    private float _baseDuration;

    private float _baseValueTakeAwayMana = 0.03f;
    private float _endValueTakeAwayMana;
    private float _chanceOfApplyBindingPoison = 0.9f;

    private bool _isActiveTalentBindingPoison = false;

    public int CurrentStacks { get => _currentStacks; set => _currentStacks = value; }
    public float StacksDuration { get => _duration; }

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.AbilitySpeed };

    public override States State => States.WitheringPoison;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        Debug.Log("Withering Enter State");

        _characterState = character;


        _duration = durationToExit;
        _baseDuration = durationToExit;

        _player = personWhoMadeBuff;

        //Debug.Log("player in WitheringPoisonState == " + _player);
        if (_player != null)
        {
            _talents = _player.CharacterState.Character.GetComponent<HeroComponent>().Talents.Talents;
            //Debug.Log("WitheringPoisonState Talent == " + _talents);

            foreach (Talent talent in _talents)
            {
                //Debug.Log("Checking talents: " + talent.name + ", Type: " + talent.GetType());
                if (talent is BindingPoison bindingPoison)
                {
                    if (_bindingPoison == null)
                    {
                        _bindingPoison = bindingPoison;
                        _isActiveTalentBindingPoison = _bindingPoison.IsActive;
                    }
                }
            }
        }

        if (_currentStacks < _maxStacks)
        {
            AddStacks();
        }
    }

    public override void UpdateState()
    {
        _timeBetweenTakeAwayMana -= Time.deltaTime;
        if (_timeBetweenTakeAwayMana <= 0)
        {
            TakeAwayMana();
            _timeBetweenTakeAwayMana = _startTimeBetweenTakeAwayMana;
        }

        if (_currentStacks <= 0)
        {
            ExitState();
        }

        _duration -= Time.deltaTime;
        if (_duration < 0 || turnOff)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        ResetValues();

        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (_currentStacks < _maxStacks)
        {
            AddStacks();
            return true;
        }
        else
        {
            _duration = _baseDuration;
            return true;
        }
    }

    public void AddStacks()
    {
        if (_currentStacks < _maxStacks)
        {
            _currentStacks++;
            //Debug.Log("if / CurrentStackPoisonBone in AddStacks == " + _currentStacks);
            _duration = _baseDuration;
        }
        else
        {
            //Debug.Log("else / CurrentStackPoisonBone in AddStacks == " + _currentStacks);
            _duration = _baseDuration;
        }
    }

    private void TakeAwayMana()
    {
        Debug.Log("WitheringPoison / TakeAwayMana");
        float takeAwayMana = _currentStacks * _baseValueTakeAwayMana;
        _endValueTakeAwayMana = _characterState.Character.Stamina.CurrentValue * takeAwayMana;

        if (_isActiveTalentBindingPoison)
        {
            if (UnityEngine.Random.Range(0.0f, 1.0f) <= _chanceOfApplyBindingPoison)
            {
                //_characterState.AddStateTest(States.BindingPoison, 10, 0, _player.gameObject, null);
            }
        }

        _characterState.Character.Stamina.ReductionCurrentValue(_endValueTakeAwayMana);
    }

    private void ResetValues()
    {
        _currentStacks = 0;
        _baseDuration = 0;
        _duration = 0;
        _endValueTakeAwayMana = 0;
        _baseValueTakeAwayMana = 1f;
        _timeBetweenTakeAwayMana = _startTimeBetweenTakeAwayMana;
    }
}
