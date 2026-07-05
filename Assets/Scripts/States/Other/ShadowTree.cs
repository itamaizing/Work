using System.Collections.Generic;
using UnityEngine;

public class ShadowTree : StackableState
{
    public override States State => States.ShadowTree;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Null;
    public override List<StatusEffect> Effects => _effects;

    private readonly List<StatusEffect> _effects = new() { StatusEffect.Ability };
    private const float _heroBonusHealthPerStack = 3f;
    private const float _minionBonusHealthPerStack = 1f;
    private float BonusPerStack => characterState.Character is HeroComponent ? _heroBonusHealthPerStack : _minionBonusHealthPerStack;

    private float _timer;
    private float _remaining;
    private bool _infinite;

    public override float RemainingDuration => _infinite ? 9999 : _remaining;

    public ShadowTree()
    {
        MaxStacksCount = 60;
    }

    public void SwitchToFinite()
    {
        _timer = 0f;
        _infinite = false;
        _remaining = Mathf.Clamp(currentStacksCount, 1, 9999);
    }

    public void SwitchToInfinite()
    {
        _infinite = true;
        _timer = 0f;
        duration = 9999;
    }

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character caster, string skillName)
    {
        characterState = character;
        personWhoMadeBuff = caster;
        _infinite = true;
        duration = 9999;
        currentStacksCount = 0;
        Stack(0);
    }

    public override void UpdateState()
    {
        if (_infinite) return;

        _timer += Time.deltaTime;
        if (_timer >= 1f)
        {
            _timer = 0f;

            if (currentStacksCount > 0)
            {
                currentStacksCount--;
                characterState.Character.Health.AddMax(-BonusPerStack);
                characterState.StateIcons.RemoveIconCount();
            }
            
            _remaining -= 1f;
            if (_remaining <= 0f || currentStacksCount <= 0) GlobalExit();
        }
    }

    public override bool Stack(float _)
    {
        if (currentStacksCount >= MaxStacksCount) return false;
        currentStacksCount++;
        characterState.Character.Health.AddMax(BonusPerStack);


        if (!_infinite) SwitchToInfinite();
        return true;
    }

    protected override void ExitState()
    {
        if (currentStacksCount > 0)  characterState.Character.Health.AddMax(-currentStacksCount * BonusPerStack);
        characterState.RemoveStateFromList(this);
    }
}
