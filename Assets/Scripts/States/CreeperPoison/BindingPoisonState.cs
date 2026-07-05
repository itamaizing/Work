using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class BindingPoisonState : StackableState
{
    private SkillManager _skillManager;

    private int _maxStacks = 1;

    private float _baseDuration;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Poison };
    public int CurrentStacks { get => currentStacksCount; set => currentStacksCount = value; }
    public float StacksDuration { get => duration; }

    public override States State => States.BindingPoison;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;

    public override List<StatusEffect> Effects => _effects;

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _skillManager = characterState.Character.Abilities;

        _baseDuration = durationToExit;
        MaxStacksCount = _maxStacks;

        if (currentStacksCount < MaxStacksCount)
        {
            AddStacks();
        }

        BlockingOrCancleingAbility();
    }

    public override void UpdateState()
    {
        if (currentStacksCount <= 0)
        {
            GlobalExit();
        }

        //Debug.Log($"BindingPoisonState / UpdateState / CharacterManager = {_skillManager}");

    }

    protected override void ExitState()
    {
        //Debug.Log($"BindingPoisonState / ExitState / CharacterManager = {_skillManager}");
        ResetValues();

        characterState.RemoveStateFromList(this);
    }

    public override bool Stack(float time)
    {
        //Debug.Log($"BindingPoisonState / Stack / CharacterManager = {_skillManager}");
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
            //Debug.Log("if / CurrentStackPoisonBone in AddStacks == " + _currentStacks);
            duration = _baseDuration;
        }
        else
        {
            //Debug.Log("else / CurrentStackPoisonBone in AddStacks == " + _currentStacks);
            duration = _baseDuration;
        }
    }

    [TargetRpc]
    private void BlockingOrCancleingAbility()
    {
        _skillManager.SkillQueue.TryCancel(true);

        if (!_skillManager.SkillQueue.TryCancel(true))
        {
            _skillManager.SkillQueue.SkillAdded += OnSkillAdded;
        }
        GlobalExit();
    }

    private void OnSkillAdded(Skill skill)
    {
        _skillManager.SkillQueue.TryCancel(true);

        _skillManager.SkillQueue.SkillAdded -= OnSkillAdded;
    }

    private void ResetValues()
    {
        currentStacksCount = 0;
        _baseDuration = 0;
        duration = 0;
    }
}
