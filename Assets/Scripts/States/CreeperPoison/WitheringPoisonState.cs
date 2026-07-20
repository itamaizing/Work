using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WitheringPoisonState : StackableState
{
    private List<Skill> _skills = new();
    private List<Talent> _talents = new();
    private BindingPoison _bindingPoison;
    private Character _player;

    private int _maxStacks = 3;

    private float _timeBetweenTakeAwayMana;
    private float _startTimeBetweenTakeAwayMana = 2f;

    private float _baseDuration;

    private float _baseValueTakeAwayMana = 0.003f;
    private float _endValueTakeAwayMana;
    private float _baseChanceOfApplyBindingPoison = 0.03f;
    private float _chanceOfApplyBindingPoison = 0.9f;

    private bool _isActiveTalentBindingPoison = false;

    public int CurrentStacks { get => currentStacksCount; set => currentStacksCount = value; }
    public float StacksDuration { get => duration; }

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Poison };
    public override States State => States.WitheringPoison;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => _effects;

    protected override void OnEnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        MaxStacksCount = _maxStacks;
        _baseDuration = durationToExit;

        _player = personWhoMadeBuff;

        if (_player != null)
        {
            _talents = _player.CharacterState.Character.GetComponent<HeroComponent>().TalentManager.ActiveTalents;

            foreach (Talent talent in _talents)
            {
                if (talent is BindingPoison bindingPoison)
                {
                    if (_bindingPoison == null)
                    {
                        _bindingPoison = bindingPoison;
                        _isActiveTalentBindingPoison = _bindingPoison.Data.IsOpen;
                    }
                }
            }
        }

        if (currentStacksCount < MaxStacksCount)
        {
            AddStacks();
        }
    }

    public override void OnUpdateState()
    {
        _timeBetweenTakeAwayMana -= Time.deltaTime;
        if (_timeBetweenTakeAwayMana <= 0)
        {
            TakeAwayMana();
            _timeBetweenTakeAwayMana = _startTimeBetweenTakeAwayMana;
        }

        if (currentStacksCount <= 0)
        {
            ExitState();
        }
    }

    protected override void OnExitState()
    {
        ResetValues();
    }

    public override bool Stack(float time)
    {
        if (currentStacksCount < MaxStacksCount)
        {
            AddStacks();
            return true;
        }
        else
        {
            duration = _baseDuration;
            return true;
        }
    }

    public void AddStacks()
    {
        if (currentStacksCount < MaxStacksCount)
        {
            currentStacksCount++;
            duration = _baseDuration;
        }
        else
        {
            duration = _baseDuration;
        }
    }

    [Server]
    private void TakeAwayMana()
    {
        float takeAwayMana = currentStacksCount * _baseValueTakeAwayMana;

        _endValueTakeAwayMana = characterState.Character.Resources[ResourceType.Mana]!.CurrentValue * takeAwayMana;

        _chanceOfApplyBindingPoison *= _baseChanceOfApplyBindingPoison;

        if (_bindingPoison != null && _isActiveTalentBindingPoison)
        {
            if (UnityEngine.Random.Range(0.0f, 1.0f) <= _chanceOfApplyBindingPoison)
            {
                characterState.AddState(States.BindingPoison, 10f, 0, _player.gameObject, null);
            }
        }

        characterState.Character.Resources[ResourceType.Mana].Add(-_endValueTakeAwayMana);
    }

    private void ResetValues()
    {
        currentStacksCount = 0;
        _baseDuration = 0;
        duration = 0;
        _endValueTakeAwayMana = 0;
        _baseValueTakeAwayMana = 1f;
        _chanceOfApplyBindingPoison = 0f;
        _timeBetweenTakeAwayMana = _startTimeBetweenTakeAwayMana;
    }
}
