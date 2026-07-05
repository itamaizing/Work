using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Calmness : RefreshingState
{
    private const float _manaRegenPercent = 0.005f;
    private const int _baseMaxStacks = 2;
    private int _lastTreesCount;
    private float _regenAmount;
    
    private Resource manaResource;
    private Coroutine _regenRoutine;

    private List<StatusEffect> _effects = new List<StatusEffect> { StatusEffect.Healing };
    public override States State => States.Calmness;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        health = character.Character.Health;
        manaResource = character.Character.TryGetResource(ResourceType.Mana);
        base.personWhoMadeBuff = personWhoMadeBuff;
        MaxStacksCount = _baseMaxStacks;
        currentStacksCount = 1;

        RecalcRegenAmount();
        if (character.isServer) _regenRoutine = character.StartCoroutine(RegenTick());
    }

    public override void UpdateState()
    {
    }

    protected override void ExitState()
    {
        currentStacksCount = 0;
        if (_regenRoutine != null) characterState.StopCoroutine(_regenRoutine);
    }

    public override bool Stack(float newDuration)
    {
        duration = Mathf.Max(duration, newDuration);

        if (currentStacksCount < MaxStacksCount)
        {
            currentStacksCount++;

            RecalcRegenAmount();
        }
        return true;
    }

    public void UpdateTreesCount(int newTreesCount)
    {
        _lastTreesCount = newTreesCount;
        MaxStacksCount = _baseMaxStacks + _lastTreesCount;

        if (currentStacksCount > MaxStacksCount) currentStacksCount = MaxStacksCount;

        RecalcRegenAmount();
    }

    public void ApplyRegen()
    {
        if (manaResource != null && _regenAmount > 0) manaResource.Add(_regenAmount);
    }

    private void RecalcRegenAmount()
    {
        if (manaResource != null) _regenAmount = manaResource.MaxValue * _manaRegenPercent * currentStacksCount;
    }

    private IEnumerator RegenTick()
    {
        var wait = new WaitForSeconds(1f);

        while (duration > 0)
        {
            yield return wait;

            if (manaResource == null) continue;
            if (!characterState.isServer) continue;

            float missing = manaResource.MaxValue - manaResource.CurrentValue;
            if (missing <= 0f) continue;

            float amount = Mathf.Min(_regenAmount, missing);
            manaResource.Add(amount);
        }
    }

}
