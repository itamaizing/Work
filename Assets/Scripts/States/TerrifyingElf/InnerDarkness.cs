using System.Collections.Generic;
using UnityEngine;

public class InnerDarkness : StateStackingRefreshing
{
    private const float TimeDecreasePerStack = 2f;

    private readonly List<StatusEffect> _effects = new List<StatusEffect> { StatusEffect.Ability };

    public override States State => States.InnerDarkness;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => _effects;

    public InnerDarkness()
    {
        SetMaxStacks(6);
    }

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character sourceCaster, string skillName)
    {
        if (sourceCaster != null && sourceCaster.TryGetComponent<TerrifyingElfAura>(out var terrifyingElfAura))
        {
            if (terrifyingElfAura.IsReductionRecharge)
            {
                SkillManager casterAbilities = sourceCaster.Abilities;
                if (casterAbilities != null && casterAbilities.Skills != null)
                {
                    foreach (Skill skill in casterAbilities.Skills)
                    {
                        if (skill == null || skill.Info == null) continue;

                        bool isDark = skill.Info.School == Schools.Dark;
                        bool isSpellish = skill.Info.AbilityForm == AbilityForm.Magic || skill.Info.AbilityForm == AbilityForm.Both;

                        if (isDark && isSpellish && skill.Cooldown != null && skill.Cooldown.IsActive)
                        {
                            float duration = skill.Cooldown.RemainingTime * 0.5f;
                            skill.Cooldown.Modify(-duration);
                        }
                    }
                }
            }
        }
    }

    public override bool Stack(float time)
    {
        if (CurrentStacksCount < MaxStacksCount)
        {
            CurrentStacksCount++;
            float calculatedDuration = Mathf.Max(0.1f, time - (CurrentStacksCount - 1) * TimeDecreasePerStack);
            RemainingDuration = calculatedDuration;

            if (CurrentStacksCount == MaxStacksCount)
            {
                ApplyFear();
            }

            return true;
        }

        if (CurrentStacksCount == MaxStacksCount)
        {
            float calculatedDuration = Mathf.Max(0.1f, time - (CurrentStacksCount - 1) * TimeDecreasePerStack);
            RemainingDuration = calculatedDuration;
            ApplyFear();
            return false;
        }

        return false;
    }

    protected override void OnExit()
    {
        CurrentStacksCount = 0;
    }

    private void ApplyFear()
    {
        if (characterState == null) return;

        float fearDuration = Random.Range(0.7f, 1.4f);
        GameObject casterObject = sourceCaster != null ? sourceCaster.gameObject : null;

        characterState.AddStateLogic(States.Fear, fearDuration, 0f, Schools.None, casterObject, null);
    }
}