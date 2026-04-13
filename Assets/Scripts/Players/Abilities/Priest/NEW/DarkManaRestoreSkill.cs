using System;
using System.Collections;
using UnityEngine;

public class DarkManaRestoreSkill : Skill, IPassiveSkill
{
    public override void LoadTargetData(TargetInfo targetInfo){ }
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback) { return null; }

    protected override IEnumerator CastJob() { return null; }

    protected override void ClearData() { }

    private bool _darkManaEnabled;

    public bool DarkManaEnabled => _darkManaEnabled;

    public void EnableDarkMana(bool value)
    {
        _darkManaEnabled = value;

        if (_darkManaEnabled)
        {
            foreach (var skill in Hero.Abilities.Abilities)
            {
                skill.OnDamageApplied += OnDarkDamageApplied;
                skill.OnHealApplied += OnLightHealApplied;
            }
        }
        else
        {
            foreach (var skill in Hero.Abilities.Abilities)
            {
                skill.OnDamageApplied -= OnDarkDamageApplied;
                skill.OnHealApplied -= OnLightHealApplied;
            }
        }
    }

    private void OnDarkDamageApplied(GameObject targetGO, Skill skill)
    {
        if(!isOwned) return;
        if (targetGO == null) return;
        if(skill.Info.School != Schools.Dark) return;
        if (!targetGO.TryGetComponent<Character>(out var target)) return;
        if (target.IsDead) return;
        var state = target.CharacterState;
        bool hasValidState = state.CheckForState(States.Stun)
                             || state.CheckForState(States.Fear)
                             || state.CheckForState(States.ShackleState);

        if (!hasValidState) return;

        if (skill.Damage <= 0f) return;

        float manaToRestore = skill.Damage;

        Resource manaResource = skill.Hero.TryGetResource(ResourceType.Mana);
        
        if (manaResource != null)
        {
            manaResource.CmdAdd(manaToRestore);
        }
    }
    
    private void OnLightHealApplied(GameObject targetGO, Skill skill)
    {
        if(isOwned) return;
        if (targetGO == null) return;
        if(skill.Info.School == Schools.Dark) return;
        if (!targetGO.TryGetComponent<Character>(out var target)) return;
        if (target.IsDead) return;

        var state = target.CharacterState;
        bool hasValidState = state.CheckForState(States.Stun)
                             || state.CheckForState(States.Fear)
                             || state.CheckForState(States.ShackleState);

        if (!hasValidState) return;

        float manaToRestore = skill.Damage;

        Resource manaResource = skill.Hero.TryGetResource(ResourceType.Mana);
        
        if (manaResource != null)
        {
            manaResource.CmdAdd(manaToRestore);
        }
    }
}
