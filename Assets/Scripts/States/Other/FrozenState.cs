using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FrozenState : RefreshingState
{
    private GameObject _frozenEffectInstance;
    private AudioSource _audioSource;
    private TalentSystem _talentSystem;
    private NinjaResources _ninjaResources;
    
    private float _baseDuration;
    private bool _isInited;

    private bool _isFrostTalentActive;

    private const int MaxStacks = 5;
    private const float MoveSlowPerStack = 0.05f;
    private const float CastSlowPerStack = 0.10f;

    private AttributeModifier _moveSpeedModifier;

    private readonly List<Skill> _affectedSkills = new();
    private float _appliedCastSlow;

    private List<StatusEffect> _effects = new() { StatusEffect.Move, StatusEffect.AbilitySpeed };

    private float _deepFrostDurability = 30f;
    private float _damageCount = 0f;
    
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override States State => States.Frozen;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;
    
    public float CurrentAttackSlowPercent => CastSlowPerStack * currentStacksCount;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        MaxStacksCount = MaxStacks;
        currentStacksCount = 1;
        duration = durationToExit;
        _baseDuration = durationToExit;
        this.damageToExit = damageToExit == 0 ? 1 : damageToExit;
        if (_ninjaResources.IsDeepFrosting)
        {
            this.damageToExit = _deepFrostDurability;
        }
        _damageCount = 0f;
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
        SubscribeOnDamage();
    }

    private void SubscribeOnDamage()
    {
        characterState.Character.Health.DamageTaken += OnDamaged;
        characterState.Character.Health.OnBeforeTakeDamage += OnDamaged;
    }
    
    private void OnDamaged(Damage damage, Skill ability)
    {
        _damageCount += damage.Value;
        if(_damageCount > damageToExit)
            ExitState();
    }

    public override void UpdateState()
    {
        bool timeExpired = duration < 0;

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
        characterState.Character.Health.DamageTaken -= OnDamaged;
        characterState.Character.Health.OnBeforeTakeDamage -= OnDamaged;
        RemoveEffects();
        _damageCount = 0f;
        currentStacksCount = 0;
        characterState.RemoveState(this);

        if (_frozenEffectInstance != null)
            _frozenEffectInstance.SetActive(false);

        foreach (var mat in characterState.StateEffects.MaterialsCharacter)
            mat.color = Color.white;
    }

    public override bool Stack(float time)
    {
        RemoveEffects();

        if (currentStacksCount < MaxStacks) currentStacksCount++;

        
        if (_ninjaResources.IsDeepFrosting)
        {
            damageToExit = _deepFrostDurability;
        }

        ApplyEffects();
        return true;
    }

    private void RestartFrozen()
    {
        duration = _baseDuration;
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
        float moveSlow = MoveSlowPerStack * currentStacksCount;

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
        _appliedCastSlow = CastSlowPerStack * currentStacksCount;

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
    
    public override AbstractCharacterState TryApply(CharacterState character, float durationToExit, 
        float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        if (!CanEnterState(character)) return null;

        if(!_ninjaResources)
            if (personWhoMadeBuff.TryGetComponent<NinjaResources>(out NinjaResources resources)) _ninjaResources = resources;

        
        if (currentStacksCount == 0)
        {
            BaseInit(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
            EnterState(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
        }
        else
        {
            float previousDuration = duration;

            BaseInit(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);

            duration = Mathf.Max(previousDuration, durationToExit);

            Stack(durationToExit);
        }

        return this;
    }
}