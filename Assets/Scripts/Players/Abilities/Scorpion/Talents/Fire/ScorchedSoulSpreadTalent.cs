using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScorchedSoulSpreadTalent : Talent
{
    [SerializeField] private ScorchedSoulSpreadNetwork _helper;

    private const float SpreadPerStack = 0.2f;

    public override void Enter()
    {
        foreach (var skill in character.Abilities.Abilities)
        {
            skill.OnDamageApplied -= OnDamageApplied;
            skill.OnDamageApplied += OnDamageApplied;
        }
    }

    public override void Exit()
    {
        foreach (var skill in character.Abilities.Abilities)
            skill.OnDamageApplied -= OnDamageApplied;
    }

    private void OnDamageApplied(GameObject target, Skill sourceSkill)
    {
        if (sourceSkill.Info.School != Schools.Fire) return;

        var primaryTarget = target.GetComponent<Character>();
        if (primaryTarget == null) return;

        
        int primaryStacks = primaryTarget.CharacterState.CheckStateStacks(States.ScorchedSoul);
        if (primaryStacks <= 0) return;

        float primarySpreadDamage = sourceSkill.Buff.Damage.GetBuffedValue(sourceSkill.Damage)
                                    * (primaryStacks * SpreadPerStack);
        if (primarySpreadDamage <= 0f) return;

        var targets = FindObjectsOfType<Character>()
            .Where(c => c.gameObject != target &&
                        !c.IsDead &&
                        c.CharacterState.CheckStateStacks(States.ScorchedSoul) > 0)
            .Select(c => c.gameObject)
            .ToList();

        if (targets.Count > 0)
            _helper.CmdApplySpreadDamage(targets, primarySpreadDamage);
    }
}
