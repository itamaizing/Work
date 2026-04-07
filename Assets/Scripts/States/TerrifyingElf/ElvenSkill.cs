using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElvenSkill : StackableState
{
    private float _duration;
    private MoveComponent _move;
    private GameObject _elvenSkillEffect;
    private TerrifyingElfAura _aura;
    private SkillManager _skillManager;

    private const float PercentBonusPerStack = 0.1f;
    private const int MaxStacks = 3;

    private int _currentStacks = 1;

    private Dictionary<Skill, (float lengthBonus, float radiusBonus)> _bonuses = new();

    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State => States.ElvenSkill;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    private List<StatusEffect> _effects = new() { StatusEffect.Ability };

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _duration = durationToExit;
        characterState = character;
        base.personWhoMadeBuff = personWhoMadeBuff;

        _move = character.GetComponent<MoveComponent>();
        _skillManager = characterState.Character.Abilities;

        _move.SetCanMoveState(true);

        _currentStacks = 1;

        ApplyBuffs();

        if (_skillManager != null)
        {
            foreach (var skill in _skillManager.Abilities)
            {
                if (skill == null) continue;

                if (skill.Info.DamageType == DamageType.Physical || skill.Info.DamageType == DamageType.Both)
                    skill.CastStarted += OnPhysCastStarted;
                else
                    skill.CastStarted += NotPhysCastStarted;

                if (skill is ReconnaissanceFire reconnaissanceFire)
                    reconnaissanceFire.TryStartElvenBoostWindow();
            }
        }

        _aura = character.GetComponent<TerrifyingElfAura>();
        if (_aura != null && _aura.ElvenSkillEffect != null)
        {
            _elvenSkillEffect = _aura.ElvenSkillEffect;
            _elvenSkillEffect.SetActive(true);
        }
    }

    public override bool Stack(float time)
    {
        _duration = time;

        if (_currentStacks >= MaxStacks)
            return false;

        _currentStacks++;

        ReapplyBuffs();

        return true;
    }

    private void ApplyBuffs()
    {
        _bonuses.Clear();

        if (_skillManager == null) return;

        float totalPercent = _currentStacks * PercentBonusPerStack;

        foreach (var skill in _skillManager.Abilities)
        {
            if (skill == null) continue;

            float baseLength = skill.AreaInfo.CastLength;
            float baseRadius = skill.AreaInfo.Radius;

            float lengthBonus = baseLength * totalPercent;
            float radiusBonus = baseRadius * totalPercent;

            skill.Buff.Length.AddValue(lengthBonus);
            skill.Buff.Radius.AddValue(radiusBonus);

            _bonuses[skill] = (lengthBonus, radiusBonus);
        }
    }

    private void RemoveBuffs()
    {
        foreach (var pair in _bonuses)
        {
            var skill = pair.Key;
            var (lengthBonus, radiusBonus) = pair.Value;

            if (skill == null) continue;

            skill.Buff.Length.RemoveValue(lengthBonus);
            skill.Buff.Radius.RemoveValue(radiusBonus);
        }

        _bonuses.Clear();
    }

    private void ReapplyBuffs()
    {
        RemoveBuffs();
        ApplyBuffs();
    }

    public override void ExitState()
    {
        if (_move) _move.SetCanMoveState(false);

        RemoveBuffs();

        if (_skillManager != null)
        {
            foreach (var skill in _skillManager.Abilities)
            {
                if (skill == null) continue;

                if (skill.Info.DamageType == DamageType.Physical || skill.Info.DamageType == DamageType.Both)
                    skill.CastStarted -= OnPhysCastStarted;
                else
                    skill.CastStarted -= NotPhysCastStarted;
            }
        }

        if (_elvenSkillEffect != null)
            _elvenSkillEffect.SetActive(false);

        characterState.StateIcons.RemoveItemByState(State);
        characterState.RemoveState(this);
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;

        if (_duration <= 0)
            ExitState();
    }

    private void OnPhysCastStarted()
    {
        if (_move) _move.SetCanMoveState(true);
    }

    private void NotPhysCastStarted()
    {
        if (_move) _move.SetCanMoveState(false);
    }
}