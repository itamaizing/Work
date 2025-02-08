using System.Collections.Generic;
using UnityEngine;

public class AstralState : AbstractCharacterState
{
    private float _baseDuration;
    private float _duration;
    private int _currentStacks = 1;
    private const int _maxStacks = 1;

    private float _originalEvadeMelee;
    private float _originalEvadeRange;
    private float _originalEvadeMagical;
    private float _originalRegenerationValue;

    private StateEffects _stateEffects;
    private Renderer _characterRenderer;
    private GameObject _weapon;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Ability, StatusEffect.Move };

    public override States State => States.Astral;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        Debug.Log("Entering Astral State");

        _characterState = character;
        _personWhoMadeBuff = personWhoMadeBuff;
        _baseDuration = durationToExit;
        _duration = _baseDuration;

        _stateEffects = _characterState.GetComponent<StateEffects>();
        if (_stateEffects == null)
        {
            Debug.LogWarning("StateEffects component is missing on character.");
            return;
        }

        _characterRenderer = _characterState.GetComponentInChildren<Renderer>();
        _weapon = _stateEffects.Weapon;

        if (_characterRenderer != null)
        {
            _characterRenderer.material = _stateEffects.MaterialGhost;
        }

        if (_weapon != null)
        {
            _weapon.SetActive(false);
        }

        _originalEvadeMelee = _characterState.Character.Health.EvadeMeleeDamage;
        _originalEvadeRange = _characterState.Character.Health.EvadeRangeDamage;
        _originalEvadeMagical = _characterState.Character.Health.ResistMagDamage;
        _originalRegenerationValue = _characterState.Character.Health.RegenerationValue;

        _characterState.Character.Health.SetEvadePhys(100);
        _characterState.Character.Health.SetEvadeMagicDecrease(10);
        _characterState.Character.Health.RegenerationValue = -Mathf.Abs(_characterState.Character.Health.RegenerationValue);
        _characterState.Character.Move.ChangeMoveSpeed(0.5f);

        BlockPhysicalAbilities();
        _characterState.Character.Health.ValueChanged += ConvertRegenToDamage;
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;
        if (_duration <= 0)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        Debug.Log("Exiting Astral State");

        _characterState.RemoveState(this);

        if (_characterRenderer != null)
        {
            _characterRenderer.material = _stateEffects.MaterialCharacter;
        }

        if (_weapon != null)
        {
            _weapon.SetActive(true);
        }

        _characterState.Character.Health.EvadeMeleeDamage = _originalEvadeMelee;
        _characterState.Character.Health.EvadeRangeDamage = _originalEvadeRange;
        _characterState.Character.Health.SetEvadeMagic(_originalEvadeMagical);
        _characterState.Character.Health.RegenerationValue = _originalRegenerationValue;
        _characterState.Character.Move.ChangeMoveSpeed(2);

        UnblockPhysicalAbilities();
        _characterState.Character.Health.ValueChanged -= ConvertRegenToDamage;
    }

    public override bool Stack(float time)
    {
        if (_currentStacks < _maxStacks)
        {
            _currentStacks++;
            _duration = _baseDuration;
            return true;
        }
        else
        {
            _duration = _baseDuration;
            return false;
        }
    }

    private void BlockPhysicalAbilities()
    {
        if (_characterState.Character.Abilities == null) return;

        foreach (var skill in _characterState.Character.Abilities.Abilities)
        {
            if (skill.AbilityForm == AbilityForm.Physical)
            {
                skill.Disactive = true;
            }
        }
    }

    private void UnblockPhysicalAbilities()
    {
        if (_characterState.Character.Abilities == null) return;

        foreach (var skill in _characterState.Character.Abilities.Abilities)
        {
            if (skill.AbilityForm == AbilityForm.Physical)
            {
                skill.Disactive = false;
            }
        }
    }

    private void ConvertRegenToDamage(float oldValue, float newValue)
    {
        if (newValue > oldValue)
        {
            float regenAmount = newValue - oldValue;
            _characterState.Character.Health.CmdAdd(regenAmount);
        }
    }
}
