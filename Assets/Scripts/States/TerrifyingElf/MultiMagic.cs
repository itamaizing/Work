using System;
using System.Collections.Generic;
using UnityEngine;

public class MultiMagic : StateStackingRefreshing
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
    private readonly Dictionary<Skill, Action> _targetCaptureHandlers = new();

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character caster, string skillName)
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

            Skill capturedSkill = skill;
            
            Action capture = () =>
            {
                var target = capturedSkill.Targeting.GetTarget()?.Character;
                if (target != null) _lastTarget = target;
            };
            capturedSkill.CastSuccess += capture;
            _targetCaptureHandlers[skill] = capture;

            if (skill.ChannelingDuration > 0)
            {
                skill.TargetingSuccess += OnTargetSkillCast;
                _castSuccessHandlers[skill] = null;
            }
            else
            {
                Action handler = () => OnTargetSkillCast(capturedSkill);
                skill.CastFinished += handler;
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

                if (_targetCaptureHandlers.TryGetValue(skill, out var capture) && capture != null)
                    skill.CastSuccess -= capture;

                if (skill.ChannelingDuration > 0)
                    skill.TargetingSuccess -= OnTargetSkillCast;
                else if (_castSuccessHandlers.TryGetValue(skill, out var handler) && handler != null)
                    skill.CastFinished -= handler;
            }
        }

        _castSuccessHandlers.Clear();
        _targetCaptureHandlers.Clear();
    }

    protected override void OnExit()
    {
        CurrentStacksCount = 0;
        UnsubscribeFromSkills();
    }

    public override bool Stack(float time) => false;

    private void OnTargetSkillCast(Skill skill)
    {
        var self = characterState.Character;

        var colliders = Physics.OverlapSphere(
            characterState.transform.position,
            skill.CastDistance,
            skill.Targeting.Layer);

        var seen = new HashSet<Character>();
        var extraCharacters = new List<Character>();

        foreach (var collider in colliders)
        {
            if (!collider.TryGetComponent(out Character character)) continue;
            if (character == self || character == _lastTarget) continue;
            if (!character.IsTargetable) continue;
            if (seen.Add(character)) extraCharacters.Add(character);
        }

        if (extraCharacters.Count > 0 && skill is IMultiMagicSkill multiSkill)
        {
            foreach (var target in extraCharacters)
                multiSkill.HandleExtraTarget(target);
        }

        _lastTarget = null;
        
        var state = characterState;
        ExitState();
        state.CmdRemoveState(State);
    }
}