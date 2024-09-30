using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpiritHealthState : AbstractCharacterState
{
    private float _baseDuration;
    private float _duration;
    private int _stacks;
    private const int MaxStacks = 2;
    private const float HealthRestorePerStack = 0.09f; // 9% health restore per stack
    private const float ManaRestorePerStack = 0.09f; // 9% mana restore per stack

    private List<StatusEffect> _effects = new ();

    public override States State => States.SpiritHealth;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _characterState = character;
        _duration = durationToExit;
        _baseDuration = durationToExit;
        _stacks = 1;
        
        ApplyManaRestore();
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;

        if (_duration <= 0 || _stacks == 0)
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
        if (_stacks >= MaxStacks)
        {
            return false;
        }

        _stacks++;
        _duration = Mathf.Max(_duration, time);

        ApplyManaRestore();

        return true;
    }

    private void ApplyManaRestore()
    {
        _characterState.Character.Resources.FirstOrDefault(o=>o.Type == ResourceType.Mana)?.Add(ManaRestorePerStack * _stacks);
    }
}