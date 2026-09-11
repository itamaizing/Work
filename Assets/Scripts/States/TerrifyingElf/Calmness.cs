using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Calmness : RefreshingStateStacking
{
    private const float _manaRegenPercent = 0.005f;
    private const int _baseMaxStacks = 2;
    private int _lastTreesCount;
    private float _regenAmount;
    private float _baseDuration;
    
    private Resource manaResource;
    private Coroutine _regenRoutine;

    private List<StatusEffect> _effects = new List<StatusEffect> { StatusEffect.Healing };
    public override States State => States.Calmness;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _baseDuration = durationToExit;
        health = character.Character.Health;
        manaResource = character.Character.Resource;
        
        SetMaxStacks(_baseMaxStacks);
        
        if (!character.isServer)
        {
            manaResource.MaxValueChanged -= RecalcRegenAmount;
            manaResource.MaxValueChanged += RecalcRegenAmount;
            _regenRoutine = character.StartCoroutine(RegenTick());
        }
    }

    public override void UpdateState()
    {
    }

    public override void ReduceStack()
    {
        RemainingDuration = _baseDuration;
        currentStacksCount--;
        RecalcRegenAmount(0, 0);
        if (currentStacksCount == 0)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        currentStacksCount = 0;
        manaResource.MaxValueChanged -= RecalcRegenAmount;
        if (_regenRoutine != null) characterState.StopCoroutine(_regenRoutine);
        
        characterState.RemoveState(this);
    }

    public override bool Stack(float newDuration)
    {
        RemainingDuration = Mathf.Max(RemainingDuration, newDuration);
        return true;
    }

    public void UpdateTreesCount(int newTreesCount)
    {
        _lastTreesCount = newTreesCount;
        SetMaxStacks(_baseMaxStacks + _lastTreesCount);

        if (currentStacksCount > MaxStacksCount) currentStacksCount = MaxStacksCount;
    }

    private void RecalcRegenAmount(float oldValue, float newValue)
    {
        if (manaResource != null)
        {
            _regenAmount = manaResource.MaxValue * _manaRegenPercent * currentStacksCount;
        }
    }

    private IEnumerator RegenTick()
    {
        var wait = new WaitForSeconds(1f);

        while (RemainingDuration > 0)
        {
            yield return wait;

            if (manaResource == null) continue;

            float missing = manaResource.MaxValue - manaResource.CurrentValue;
            if (missing <= 0f) continue;

            float amount = Mathf.Min(_regenAmount, missing);
            manaResource.CmdAdd(amount);
        }
    }
    
    

}
