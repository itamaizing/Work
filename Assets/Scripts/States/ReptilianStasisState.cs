using System.Collections.Generic;
using UnityEngine;

public class ReptilianStasisState : StackableState
{
    private Character _owner;

    public override States State => States.ReptilianStasis;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;

    public override List<StatusEffect> Effects => new()
    {
        StatusEffect.Stunning,
    };

    protected override void OnEnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _owner = character.GetComponent<Character>();
        if (_owner == null) return;

        MaxStacksCount = 1;

        ApplyStasis();
    }

    private void ApplyStasis()
    {
        _owner.Move.SetCanMove(false);
        _owner.Move.IsMoveBlocked = true;

        _owner.Abilities.SetAbilitiesDisactive(true);
        _owner.Abilities.CancleAllSkills();
    }

    protected override void OnExitState()
    {
        RemoveStasis();
        ResetCooldowns();

    }

    private void RemoveStasis()
    {
        if (_owner == null) return;

        _owner.Move.SetCanMove(true);
        _owner.Move.IsMoveBlocked = false;

        _owner.Abilities.SetAbilitiesDisactive(false);
    }

    private void ResetCooldowns()
    {
        if (_owner == null) return;

        foreach (var skill in _owner.Abilities.Skills)
        {
            if (skill == null) continue;

            if (skill.Cooldown.IsActive)
            {
                skill.Cooldown.ForceEnd();
            }
        }
    }

    public override bool Stack(float time)
    {
        duration = time;
        return false;
    }

    public override void OnUpdateState() { }
}