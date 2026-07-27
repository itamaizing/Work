using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AstralState : RefreshingState
{
    private float _defMagDamageMod = 50f;
    private float _originalRegenerationValue;

    private StateEffects _stateEffects;
    private SkinnedMeshRenderer _characterRenderer;
    private GameObject _weapon;
    private Renderer _weaponRenderer;
    private Material _originalWeaponMaterial;
    private Material[] _originalMaterials;

    private Coroutine _dotJob;
    private AttributeModifier _moveMod = new AttributeModifier(-0.5f, ModifierType.Multiplier);
    private AttributeModifier _regenMod = new AttributeModifier(-1f, ModifierType.Percent);
    private AttributeModifier _magResMod;
    private AttributeModifier _physResMod;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Ability, StatusEffect.Move };
    private readonly Dictionary<Skill, AttributeModifier> _skillModifiers = new();

    public override States State => States.Astral;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        currentStacksCount = 1;
        
        _moveMod.Value = -0.5f;
        _moveMod.Type = ModifierType.Multiplier;
        _moveMod.Source = this;

        _regenMod.Value = -1f;
        _regenMod.Type = ModifierType.Percent;
        _regenMod.Source = this;

        _magResMod = new AttributeModifier(-_defMagDamageMod, ModifierType.Flat, this);

        _stateEffects = characterState.GetComponent<StateEffects>();
        if (_stateEffects == null)
        {
            return;
        }

        _characterRenderer = characterState.GetComponentInChildren<SkinnedMeshRenderer>();
        _weapon = _stateEffects.Weapon;

        if (_characterRenderer != null)
        {
            _originalMaterials = _characterRenderer.materials;
            Material[] ghostMaterials = new Material[_originalMaterials.Length];
            for (int i = 0; i < ghostMaterials.Length; i++)
            {
                ghostMaterials[i] = _stateEffects.MaterialGhost;
            }
            _characterRenderer.materials = ghostMaterials;
        }

        if (_weapon != null && (_weaponRenderer = _weapon.GetComponent<Renderer>()) != null)
        {
            _originalWeaponMaterial = _weaponRenderer.material;
            _weaponRenderer.material = _stateEffects.MaterialGhost;
        }

        var characterHealth = characterState.Character.Health;
        _originalRegenerationValue = characterHealth.RegenerationValue;

        characterHealth.AddModifier(ResourceAttributeName.Regen, _regenMod);
        
        var attributes = characterState.Character.AttributeSystem;
        if (attributes != null)
        {
            attributes[CharacterAttributeName.ResistanceMagical].AddModifier(_magResMod);

            float currentPhysRes = attributes[CharacterAttributeName.ResistancePhysical].GetValue();
            _physResMod = new AttributeModifier(100f - currentPhysRes, ModifierType.Flat, this);
            attributes[CharacterAttributeName.ResistancePhysical].AddModifier(_physResMod);
        }

        characterState.Character.Move.AddModifier(_moveMod);

        BlockPhysicalAbilities();

        foreach (var skill in characterState.Character.Abilities.Abilities)
        {
            if (skill.Info.AbilityForm == AbilityForm.Magic || skill.Info.AbilityForm == AbilityForm.Both)
            {
                var damageMod = new AttributeModifier(0.5f, ModifierType.Percent, this);
                skill.Attributes[SkillAttributeName.Damage].AddModifier(damageMod);
                _skillModifiers[skill] = damageMod;
            }
        }

        if (characterState.isServer) _dotJob = characterState.StartCoroutine(DotJob());
    }

    public override void UpdateState()
    {
    }

    public override void ExitState()
    {
        characterState.RemoveState(this);

        if (_characterRenderer != null) _characterRenderer.materials = _originalMaterials;
        if (_weapon != null) _weaponRenderer.material = _originalWeaponMaterial;

        var characterHealth = characterState.Character.Health;
        characterHealth.RemoveModifierBySource(ResourceAttributeName.Regen, this);

        var attributes = characterState.Character.AttributeSystem;
        if (attributes != null)
        {
            attributes[CharacterAttributeName.ResistanceMagical].RemoveBySource(this);
            if (_physResMod != null)
            {
                attributes[CharacterAttributeName.ResistancePhysical].RemoveBySource(this);
            }
        }

        characterState.Character.Move.RemoveModifier(_moveMod);

        if (_dotJob != null) characterState.StopCoroutine(_dotJob);
        UnblockPhysicalAbilities();

        foreach (var kvp in _skillModifiers)
        {
            kvp.Key.Attributes[SkillAttributeName.Damage].RemoveBySource(this);
        }
        _skillModifiers.Clear();
    }

    private void BlockPhysicalAbilities()
    {
        foreach (var skill in characterState.Character.Abilities.Abilities)
            if (skill.Info.AbilityForm == AbilityForm.Physical) skill.Disactive = true;
    }

    private void UnblockPhysicalAbilities()
    {
        foreach (var skill in characterState.Character.Abilities.Abilities)
            if (skill.Info.AbilityForm == AbilityForm.Physical) skill.Disactive = false;
    }

    private IEnumerator DotJob()
    {
        float period = characterState.Character.Health.RegenerationPeriod;
        if (period <= 0) period = 1f;

        while (true)
        {
            yield return new WaitForSeconds(period);

            float value = _originalRegenerationValue;
            if (value <= 0) continue;

            Damage damage = new Damage { Value = value, Type = DamageType.Magical };
            characterState.Character.Health.TryTakeDamage(ref damage, null);
        }
    }
    
    public override bool Stack(float time)
    {
        duration = BaseDurationValue;
        return false;
    }
    
    public override AbstractCharacterState TryApply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        if (!CanEnterState(character)) return null;

        BaseInit(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);

        if (currentStacksCount == 0)
        {
            EnterState(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
        }
        else
        {
            Stack(duration);
        }

        return this;
    }
}