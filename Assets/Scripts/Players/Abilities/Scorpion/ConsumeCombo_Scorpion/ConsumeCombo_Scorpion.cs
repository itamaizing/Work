using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ConsumeCombo_Scorpion : Skill
{
    private List<Character> _comboTargetsQueue = new List<Character>();

    public int AvailablePoints => _comboTargetsQueue.Sum(target =>
    {
        var state = target.CharacterState.GetState(States.ComboState) as ComboState;
        return state?.CurrentStacksCount ?? 0;
    });

    protected override bool IsCanCast => true;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    public void ApplyComboEffect(Transform enemy)
    {
        if (!isServer) return;

        if (enemy == null) return;

        var targetCharacter = enemy.GetComponent<Character>();
        if (targetCharacter == null) return;

        var stateManager = targetCharacter.CharacterState;
        if (stateManager == null) return;

        if (!stateManager.CheckForState(States.ComboState))
        {
            _comboTargetsQueue.Add(targetCharacter);
        }

        Debug.Log($"Накладываем ComboState на {enemy.name}");

        stateManager.CmdAddState(States.ComboState, 999f, 0f, _hero.gameObject, nameof(ConsumeCombo_Scorpion));
    }

    public int PayComboPoints(int amount, Character specificTarget = null)
    {
        int pointsConsumed = 0;

        if (specificTarget != null)
        {
            pointsConsumed = ConsumePointsFromTarget(specificTarget, amount);
        }
        else
        {
            pointsConsumed = ConsumePointsFromQueue(amount);
        }

        return pointsConsumed;
    }

    private int ConsumePointsFromTarget(Character target, int amount)
    {
        if (target == null) return 0;

        var state = target.CharacterState.GetState(States.ComboState) as ComboState;
        if (state == null) return 0;

        int availablePoints = state.CurrentStacksCount;
        int pointsToConsume = Mathf.Clamp(amount, 0, availablePoints);

        for (int i = 0; i < pointsToConsume; i++)
        {
            bool reduced = state.Stack(-1);
            if (!reduced)
            {
                target.CharacterState.RemoveState(state);
                _comboTargetsQueue.Remove(target);
                break;
            }
        }

        return pointsToConsume;
    }

    private int ConsumePointsFromQueue(int amount)
    {
        int pointsToConsume = 0;

        while (amount > 0 && _comboTargetsQueue.Count > 0)
        {
            var lastTarget = _comboTargetsQueue[_comboTargetsQueue.Count - 1];
            var state = lastTarget.CharacterState.GetState(States.ComboState) as ComboState;

            if (state == null)
            {
                _comboTargetsQueue.RemoveAt(_comboTargetsQueue.Count - 1);
                continue;
            }

            bool reduced = state.Stack(-1);
            pointsToConsume++;
            amount--;

            if (!reduced)
            {
                lastTarget.CharacterState.RemoveState(state);
                _comboTargetsQueue.RemoveAt(_comboTargetsQueue.Count - 1);
            }
        }

        return pointsToConsume;
    }

    protected override IEnumerator PrepareJob() => null;
    protected override IEnumerator CastJob() => null;
    protected override void ClearData() { }
}
