using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BindingPoisonState : AbstractCharacterState
{
    public bool turnOff = false;

    private SkillManager _skillManager;

    private int _currentStacks = 0;
    private int _maxStacks = 1;

    private float _duration;
    private float _baseDuration;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.AbilitySpeed };

    public int CurrentStacks { get => _currentStacks; set => _currentStacks = value; }
    public float StacksDuration { get => _duration; }

    public override States State => States.BindingPoison;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        Debug.Log($"BindingPoisonState / EnterState");

        _characterState = character;

        _skillManager = _characterState.Character.Abilities;

        _duration = durationToExit;
        _baseDuration = durationToExit;

        if (_currentStacks < _maxStacks)
        {
            AddStacks();
        }

        BlockingOrCancleingAbility();
    }

    public override void UpdateState()
    {
        if (_currentStacks <= 0)
        {
            ExitState();
        }

        //Debug.Log($"BindingPoisonState / UpdateState / CharacterManager = {_skillManager}");
        if (_duration < 0 || turnOff)
        {
            ExitState();
        }

    }

    public override void ExitState()
    {
        //Debug.Log($"BindingPoisonState / ExitState / CharacterManager = {_skillManager}");
        ResetValues();

        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        //Debug.Log($"BindingPoisonState / Stack / CharacterManager = {_skillManager}");
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

    [ClientRpc]
    private void BlockingOrCancleingAbility()
    {
        Debug.Log("BindingPoison / BlockingOrCancleingAbility");
        Debug.Log($"BindingPoisonState / BlockingOrCancleingAbility / CharacterManager = {_skillManager}");

        _skillManager.SkillQueue.TryCancel(true);
            Debug.Log($"BindingPoison / BlockingOrCancleingAbility / skillManager.TryCancel = {_skillManager.SkillQueue.TryCancel(true)}");

        if (!_skillManager.SkillQueue.TryCancel(true))
        {
            Debug.Log("BindingPoison / BlockingOrCancleingAbility / TryCancel = false");
            _skillManager.SkillQueue.SkillAdded += OnSkillAdded;
            Debug.Log($"BindingPoison / BlockingOrCancleingAbility / after SkillAdded += OnSkillAdded");
        }
        ExitState();
    }

    private void OnSkillAdded(Skill skill)
    {
        Debug.Log("BindingPoison / OnSkillAdded Start");
        Debug.Log($"BindingPoison / OnSkillAdded / CurrentSkill = {_skillManager.SkillQueue.CurrentSkill}");
        _skillManager.SkillQueue.TryCancel(true);

        _skillManager.SkillQueue.SkillAdded -= OnSkillAdded;
        Debug.Log("BindingPoison / OnSkillAdded End");
    }

    private void ResetValues()
    {
        _currentStacks = 0;
        _baseDuration = 0;
        _duration = 0;
    }
}
