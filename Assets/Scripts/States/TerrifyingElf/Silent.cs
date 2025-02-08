using System.Collections.Generic;
using UnityEngine;

public class Silent : AbstractCharacterState
{
    private float _baseDuration;
    private int _currentStacks = 1;
    private const int _maxStacks = 1;
    private float _duration;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Ability };
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override States State => States.Silent;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        Debug.Log("Entering Silent State");
        _characterState = character;
        _personWhoMadeBuff = personWhoMadeBuff;
        _baseDuration = durationToExit;
        _duration = _baseDuration;

        BlockMagicAbilities();
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;
        if (_duration <= 0)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        Debug.Log("Exiting Silent State");
        _characterState.RemoveState(this);

        UnblockMagicAbilities();
    }

    public override bool Stack(float time)
    {
        if (_currentStacks < _maxStacks)
        {
            _currentStacks++;
            _duration = _baseDuration;
            Debug.Log($"Stacking Silent. Current stacks: {_currentStacks}, New duration: {_duration}s");
            return true;
        }
        else
        {
            _duration = _baseDuration;
            Debug.Log($"Max stacks reached. Refreshing Silent duration: {_duration}s");
            return false;
        }
    }

    private void BlockMagicAbilities()
    {
        if (_characterState.Character.Abilities == null) return;

        foreach (var skill in _characterState.Character.Abilities.Abilities)
        {
            if (skill.AbilityForm == AbilityForm.Magic || skill.AbilityForm == AbilityForm.Spell)
            {
                skill.Disactive = true;
                Debug.Log($"Blocking magic skill: {skill.Name}");
            }
        }
    }

    private void UnblockMagicAbilities()
    {
        if (_characterState.Character.Abilities == null) return;

        foreach (var skill in _characterState.Character.Abilities.Abilities)
        {
            if (skill.AbilityForm == AbilityForm.Magic || skill.AbilityForm == AbilityForm.Spell)
            {
                skill.Disactive = false;
                Debug.Log($"Unblocking magic skill: {skill.Name}");
            }
        }
    }
}
