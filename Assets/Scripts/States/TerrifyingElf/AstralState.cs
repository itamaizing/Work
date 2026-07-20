using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AstralState : RefreshingState
{
    private float _defMagDamageMod = 50f;
    private float _originalRegenerationValue;
    private float _originalDefPhysDamage;

    private StateEffects _stateEffects;
    private SkinnedMeshRenderer _characterRenderer;
    private GameObject _weapon;
    private Renderer _weaponRenderer;
    private Material _originalWeaponMaterial;
    private Material[] _originalMaterials;

    private Coroutine _dotJob;
    private AttributeModifier _modif = new AttributeModifier(0.5f,ModifierType.Multiplier);

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Ability, StatusEffect.Move };
    private readonly Dictionary<Skill, float> _modifiedSkills = new();

    public override States State => States.Astral;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        currentStacksCount = 1;
        
        _modif.Value = -0.5f;
        _modif.Type = ModifierType.Multiplier;

        _stateEffects = characterState.GetComponent<StateEffects>();
        if (_stateEffects == null)
        {
            Debug.LogWarning("StateEffects component is missing on character.");
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

        _originalDefPhysDamage = characterHealth.DefPhysDamage;
        _originalRegenerationValue = characterHealth.RegenerationValue;
        characterHealth.DefMagDamage -= _defMagDamageMod;
        characterHealth.DefPhysDamage = 100;

        characterState.Character.Health.RegenerationValue = 0;


       // characterState.Character.Move.ChangeMoveSpeed(0.5f);
        characterState.Character.Move.AddModifier(_modif);

        BlockPhysicalAbilities();

        foreach (var skill in characterState.Character.Abilities.Abilities)
        {
            if (skill.Info.AbilityForm == AbilityForm.Magic || skill.Info.AbilityForm == AbilityForm.Both)
            {
                _modifiedSkills[skill] = skill.Damage;
                skill.Damage *= 1.5f;
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

        characterHealth.DefMagDamage += _defMagDamageMod;
        characterHealth.DefPhysDamage = _originalDefPhysDamage;

        characterState.Character.Move.RemoveModifier(_modif);
        //characterState.Character.Move.ChangeMoveSpeed(2);

        if (_dotJob != null) characterState.StopCoroutine(_dotJob);
        characterHealth.RegenerationValue = _originalRegenerationValue;
        UnblockPhysicalAbilities();

        foreach (var (skill, baseDamage) in _modifiedSkills) skill.Damage = baseDamage;
        _modifiedSkills.Clear();
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
