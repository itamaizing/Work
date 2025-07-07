using Mirror;
using System.Collections;
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
    private SkinnedMeshRenderer _characterRenderer;
    private GameObject _weapon;
    private Renderer _weaponRenderer;
    private Material _originalWeaponMaterial;
    private Material[] _originalMaterials;

    private Coroutine _dotJob;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Ability, StatusEffect.Move };

    public override States State => States.Astral;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
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

        _characterRenderer = _characterState.GetComponentInChildren<SkinnedMeshRenderer>();
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

        _originalEvadeMelee = _characterState.Character.Health.EvadeMeleeDamage;
        _originalEvadeRange = _characterState.Character.Health.EvadeRangeDamage;
        _originalEvadeMagical = _characterState.Character.Health.ResistMagDamage;
        _originalRegenerationValue = _characterState.Character.Health.RegenerationValue;

        _characterState.Character.Health.SetEvadePhys(100);
        _characterState.Character.Health.SetEvadeMagicDecrease(10);
        _characterState.Character.Health.RegenerationValue = 0;
        _characterState.Character.Move.ChangeMoveSpeed(0.5f);

        BlockPhysicalAbilities();

        if (_characterState.isServer) _dotJob = _characterState.StartCoroutine(DotJob());
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

        if (_characterRenderer != null) _characterRenderer.materials = _originalMaterials;
        if (_weapon != null) _weaponRenderer.material = _originalWeaponMaterial;

        _characterState.Character.Health.EvadeMeleeDamage = _originalEvadeMelee;
        _characterState.Character.Health.EvadeRangeDamage = _originalEvadeRange;
        _characterState.Character.Health.SetEvadeMagic(_originalEvadeMagical);
        _characterState.Character.Move.ChangeMoveSpeed(2);

        if (_dotJob != null) _characterState.StopCoroutine(_dotJob);
        _characterState.Character.Health.RegenerationValue = _originalRegenerationValue;
        UnblockPhysicalAbilities();

        _characterState.RemoveState(this);
    }

    private void BlockPhysicalAbilities()
    {
        foreach (var skill in _characterState.Character.Abilities.Abilities)
            if (skill.AbilityForm == AbilityForm.Physical) skill.Disactive = true;
    }

    private void UnblockPhysicalAbilities()
    {
        foreach (var skill in _characterState.Character.Abilities.Abilities)
            if (skill.AbilityForm == AbilityForm.Physical) skill.Disactive = false;
    }

    public override bool Stack(float time)
    {
        if (_currentStacks < _maxStacks) _currentStacks++;

        _duration = _baseDuration;
        return true;
    }

    private IEnumerator DotJob()
    {
        float period = _characterState.Character.Health.RegenerationDelay;
        if (period <= 0) period = 1f;

        while (true)
        {
            yield return new WaitForSeconds(period);

            float damage = _originalRegenerationValue;
            if (damage > 0) _characterState.Character.Health.TryUse(damage);
        }
    }
}
