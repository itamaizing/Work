using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FrozenState : AbstractCharacterState
{
    private GameObject _frozenEffectInstance;
    private AudioSource _audioSource;
    private TalentSystem _talentSystem;

    private float _duration;
    private float _baseDuration;
    private float _damageToExit;
    private float _damageOnStart;
    private bool _isInited;

    private bool _isFrostTalentActive;

    private int _currentStacks = 1;

    private const int MaxStacks = 5;
    private const float MoveSlowPerStack = 0.05f;
    private const float CastSlowPerStack = 0.10f;

    private AttributeModifier _moveSpeedModifier;

    private readonly List<Skill> _affectedSkills = new();
    private float _appliedCastSlow;

    private List<StatusEffect> _effects = new() { StatusEffect.Move, StatusEffect.AbilitySpeed };

    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override States State => States.Frozen;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    public int CurrentStacks => _currentStacks;
    public float CurrentAttackSlowPercent => CastSlowPerStack * _currentStacks;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        MaxStacksCount = MaxStacks;

        _duration = durationToExit;
        _baseDuration = durationToExit;
        _damageToExit = damageToExit == 0 ? 10000 : damageToExit;
        _damageOnStart = characterState.Character.Health.SumDamageTaken;

        _audioSource = character.GetComponent<AudioSource>();

        if (personWhoMadeBuff.TryGetComponent<TalentSystem>(out TalentSystem talentSystem)) _talentSystem = talentSystem;
        if (_talentSystem != null) _isFrostTalentActive = _talentSystem.ActiveTalents.Any(t => t.GetType().Name == "FrostTalent_12");

        if (character.TryGetComponent<Character>(out var abilityCharacter))
        {
            abilities = abilityCharacter.Abilities;
        }

        ApplyEffects();

        if (characterState.StateEffects.FrozenStateEffect != null)
        {
            _frozenEffectInstance = characterState.StateEffects.FrozenStateEffect;
            _frozenEffectInstance.SetActive(true);
        }

        foreach (var mat in characterState.StateEffects.MaterialsCharacter)
            mat.color = Color.cyan;

        if (characterState.StateEffects.FrozenAudio != null && _audioSource != null)
            _audioSource.PlayOneShot(characterState.StateEffects.FrozenAudio);

        _isInited = true;
    }

    public override void UpdateState()
    {
        bool timeExpired = _duration < 0;
        bool damageExceeded = characterState.Character.Health.SumDamageTaken - _damageOnStart >= _damageToExit;

        if (damageExceeded)
        {
            ExitState();
            return;
        }

        if (timeExpired)
        {
            if (_isFrostTalentActive)
            {
                RestartFrozen();
                return;
            }

            ExitState();
        }
    }

    public override void ExitState()
    {
        RemoveEffects();

        characterState.RemoveState(this);

        if (_frozenEffectInstance != null)
            _frozenEffectInstance.SetActive(false);

        foreach (var mat in characterState.StateEffects.MaterialsCharacter)
            mat.color = Color.white;
    }

    public override bool Stack(float time)
    {
        RemoveEffects();

        if (_currentStacks < MaxStacks) _currentStacks++;

        if (_damageToExit < 30) _damageToExit = 30;
        _duration = time;

        ApplyEffects();
        return true;
    }

    private void RestartFrozen()
    {
        _duration = _baseDuration;
    }

    private void ApplyEffects()
    {
        ApplyMoveSlow();
        ApplyCastSlow();
    }

    private void RemoveEffects()
    {
        RemoveMoveSlow();
        RemoveCastSlow();
    }

    private void ApplyMoveSlow()
    {
        float moveSlow = MoveSlowPerStack * _currentStacks;

        _moveSpeedModifier = new AttributeModifier(-moveSlow, ModifierType.Multiplier, this);
        characterState.Character.Move.AddModifier(_moveSpeedModifier);
    }

    private void RemoveMoveSlow()
    {
        if (_moveSpeedModifier != null)
        {
            characterState.Character.Move.RemoveModifier(_moveSpeedModifier);
            _moveSpeedModifier = null;
        }
    }

    private void ApplyCastSlow()
    {
        _affectedSkills.Clear();
        _appliedCastSlow = CastSlowPerStack * _currentStacks;

        if (abilities == null) return;

        foreach (var ability in abilities.Abilities)
        {
            if (ability != null && ability.Info.AbilityForm == AbilityForm.Physical)
            {
                ability.Buff.CastSpeed.ReductionPercentage(_appliedCastSlow);
                _affectedSkills.Add(ability);
            }
        }
    }

    private void RemoveCastSlow()
    {
        foreach (var ability in _affectedSkills)
        {
            if (ability != null)
            {
                ability.Buff.CastSpeed.IncreasePercentage(_appliedCastSlow);
            }
        }

        _affectedSkills.Clear();
        _appliedCastSlow = 0f;
    }
}