using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MultiMagic : RefreshingState
{
    private readonly List<StatusEffect> _effects = new() { StatusEffect.Ability };

    private SkillManager _skills;
    private Character _lastTarget;

    public override States State => States.MultiMagic;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public Character LastTarget { get => _lastTarget; set => _lastTarget = value; }  
    
    private readonly Dictionary<Skill, Action> _castSuccessHandlers = new();

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character caster, string skillName)
    {
        BaseInit(character, durationToExit, damageToExit, caster, skillName);
        _skills = caster.GetComponent<SkillManager>();

        if (character.isServer && !character.isClient) return;

        SubscribeToSkills();
    }

    private void SubscribeToSkills()
    {
        if (_skills == null || _skills.Abilities == null) return;

        foreach (var skill in _skills.Abilities)
        {
            if (skill is not IMultiMagicSkill) continue;
            if (_castSuccessHandlers.ContainsKey(skill)) continue;

            if (skill.CastStreamDuration > 0)
            {
                skill.PreparingSuccess += OnTargetSkillCast;
                _castSuccessHandlers[skill] = null;
            }
            else
            {
                Skill capturedSkill = skill;
                Action handler = () => OnTargetSkillCast(capturedSkill);
                skill.CastSuccess += handler;
                _castSuccessHandlers[skill] = handler;
            }
        }
    }
    
    private void UnsubscribeFromSkills()
    {
        if (_skills != null && _skills.Abilities != null)
        {
            foreach (var skill in _skills.Abilities)
            {
                if (skill is not IMultiMagicSkill) continue;

                if (skill.CastStreamDuration > 0)
                    skill.PreparingSuccess -= OnTargetSkillCast;
                else if (_castSuccessHandlers.TryGetValue(skill, out var handler) && handler != null)
                    skill.CastSuccess -= handler;
            }
        }

        _castSuccessHandlers.Clear();
    }
    
    public override void UpdateState()
    {
    }

    public override void ExitState()
    {
        currentStacksCount = 0;
        UnsubscribeFromSkills();

        base.ExitState();
    }

    public override bool Stack(float time) => false;

    private void OnTargetSkillCast(Skill skill)
    {
        float distance = skill.AreaInfo.Radius;
        LayerMask targetsMask = skill.Targeting.Layer;

        var colliders = Physics.OverlapSphere(characterState.transform.position, distance, targetsMask);
        var extraCharacters = new List<Character>();

        foreach (var collider in colliders)
        {
            if (collider.TryGetComponent(out Character character) && character != characterState.Character && character != _lastTarget)
            {
                extraCharacters.Add(character);
            }
        }

        if (extraCharacters.Count > 0 && skill is IMultiMagicSkill multiSkill)
        {
            foreach (var target in extraCharacters)
            {
                multiSkill.HandleExtraTarget(target);
            }
        }
        
        ExitState();
    }
}