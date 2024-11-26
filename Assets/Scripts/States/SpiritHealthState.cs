using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpiritHealthState : AbstractCharacterState
{
    private float _baseDuration;
    private float _duration;
    private bool _isTalentActive = false;
    private const float ManaRestorePerStack = 0.09f;
    private const float BuffedManaRestorePerStack = 0.18f;

    private List<StatusEffect> _effects = new ();

    public override float TEST_ChangeableValue { get; set; }
    public override States State => States.SpiritHealth;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _characterState = character;
        _duration = durationToExit;
        _baseDuration = durationToExit;
        _isTalentActive = damageToExit > 0;
        CurrentStacksCount++;
        MaxStacksCount = 2;
        ApplyManaRestore();
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;
        
        if (_duration <= _baseDuration * (CurrentStacksCount - 1) && CurrentStacksCount > 0)
        {
            CurrentStacksCount--;
            _duration = _baseDuration * CurrentStacksCount;

            if (CurrentStacksCount == 0)
            {
                ExitState();
            }
        }
    }

    public override void ExitState()
    {
        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (CurrentStacksCount < MaxStacksCount)
        {
            CurrentStacksCount++;
            _duration += time;
            _duration = Mathf.Min(_duration, _baseDuration * CurrentStacksCount);
            ApplyManaRestore();
        }

        return true;
    }

    private void ApplyManaRestore()
    {
        var manaRestoreValue = _isTalentActive ? BuffedManaRestorePerStack : ManaRestorePerStack;
        _characterState.Character.Resources.FirstOrDefault(o=>o.Type == ResourceType.Mana)?.Add(manaRestoreValue * CurrentStacksCount);
    }
}