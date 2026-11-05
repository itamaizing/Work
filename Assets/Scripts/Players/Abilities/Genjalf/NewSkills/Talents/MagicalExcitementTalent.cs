using System.Collections.Generic;
using UnityEngine;

public class MagicalExcitementTalent : Talent
{
    [SerializeField]
    private float _increaseManaRegenerationDuration = 3f;

    private const float DefaultDuration = 3f;

    private readonly HashSet<Skill> _subscribedSkills = new();
    private bool _isActive;

    public override void Enter()
    {
        if (_isActive || character == null || !character.isOwned)
            return;

        _isActive = true;

        character.Abilities.SkillAdded += Subscribe;
        character.Abilities.SkillRemoved += Unsubscribe;

        foreach (var skill in character.Abilities.Abilities)
            Subscribe(skill);
    }

    public override void Exit()
    {
        if (!_isActive)
            return;

        _isActive = false;

        character.Abilities.SkillAdded -= Subscribe;
        character.Abilities.SkillRemoved -= Unsubscribe;

        foreach (var skill in _subscribedSkills)
        {
            if (skill != null)
                skill.OnDamageApplied -= OnSpellHit;
        }

        _subscribedSkills.Clear();
    }

    private void Subscribe(Skill skill)
    {
        if (skill == null)
            return;

        if (skill.Info.AbilityForm != AbilityForm.Magic && skill.Info.AbilityForm != AbilityForm.Mixed) return;
        

        if (_subscribedSkills.Add(skill)) skill.OnDamageApplied += OnSpellHit;
    }

    private void Unsubscribe(Skill skill)
    {
        if (skill != null && _subscribedSkills.Remove(skill))
            skill.OnDamageApplied -= OnSpellHit;
    }

    private void OnSpellHit(GameObject target, Skill skill)
    {
        if (!_isActive || !character.isOwned || target == null || skill == null) return;

        character.CharacterState.CmdAddState(States.MagicalExcitement, _increaseManaRegenerationDuration, 0f, character.gameObject, nameof(MagicalExcitementTalent));
    }

    public void IncreaseDuration(float newDuration)
    {
        _increaseManaRegenerationDuration = newDuration;
    }

    public void SetDefaultDuration()
    {
        _increaseManaRegenerationDuration = DefaultDuration;
    }

    private void OnDestroy()
    {
        Exit();
    }
}