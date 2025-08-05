using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MultiMagic : AuraState
{
    private readonly List<StatusEffect> _effects = new() { StatusEffect.Ability };

    private SkillManager _skills;
    private Skill _pendingSkill;
    private Character _lastTarget;
    private readonly List<Vector3> _pending = new();

    private float _distance;
    private LayerMask _targetsMask;

    public override States State => States.MultiMagic;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public override float Distance => _distance;
    public override float EffectRate => 1f;
    public override LayerMask LayerMask => _targetsMask;
    public override float RemainingDuration => duration;

    public Character LastTarget { get => _lastTarget; set => _lastTarget = value; }  

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character caster, string skillName)
    {
        _characterState = character;
        _skills = caster.GetComponent<SkillManager>();
        duration = durationToExit;

        foreach (var skill in _skills.Abilities.Where(ability => ability.SkillType == SkillType.Target)) skill.CastSuccessSkill += OnTargetSkillCast;
    }

    public override void UpdateState()
    {
        duration -= Time.deltaTime;
        if (duration <= 0f) ExitState();
    }

    public override void ExitState()
    {
        foreach (var skill in _skills.Abilities.Where(ability => ability.SkillType == SkillType.Target)) skill.CastSuccessSkill -= OnTargetSkillCast;
        Debug.Log("выход из мульти");
        _characterState.RemoveState(this);
    }

    public override bool Stack(float time) => false;
    public override void EffectOnEnter(Character character) { }
    public override void EffectOnExit(Character character) { }

    public override void EffectOnStay(List<Character> characters)
    {
        if (_pendingSkill == null) return;

        foreach (var character in characters)
        {
            if (character == _characterState.Character) continue;

            _pending.Add(character.transform.position);
        }

        _pendingSkill = null;
    }

    public List<Vector3> PopPendingTargets()
    {
        var list = new List<Vector3>(_pending);
        _pending.Clear();
        return list;
    }

    private void OnTargetSkillCast(Skill skill)
    {
         Debug.Log("вызов CastSuccessSkill");

        _pending.Clear();

        _distance = skill.Radius;
        _targetsMask = skill.TargetsLayers;

        var colliders = Physics.OverlapSphere(_characterState.transform.position, _distance, _targetsMask);

        foreach (var collider in colliders) if (collider.TryGetComponent(out Character character) && character != _characterState.Character && character != _lastTarget)
                _pending.Add(character.transform.position);
    }

}
