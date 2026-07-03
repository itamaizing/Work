using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class FrostEnergy : Skill
{
    [SerializeField] private float _runeCost = 1f;

    private Coroutine _drainRoutine;
    private RuneComponent _rune;

    private const float StartDelay = 2f;
    private const float DrainInterval = 0.1f;
    private const float EnergyPerTick = 1f;
    private const float FrostEnergyCoolingBonusPerStack = 1f;
    private const float FrostEnergyFrostingBonus = 5f;
    private const float FrostEnergyFrozenBonus = 10f;
    private const float FrostEnergyPhysicalCoolingChance = 60f;
    private const float SelfCastingThreshold = 0.2f;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => true;
    public bool HeroHasFrostEnergy => Hero.CharacterState.CheckForState(States.FrostEnergy);
    
    private bool _isSelfActivating = false;

    #region Talent

    private bool _isUseRuneBonusEffect;

    public void _UseRuneBonusEffect(bool value) => _isUseRuneBonusEffect = value;
    #endregion

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);
        AddResourceTypeRune();
        SubscribeForAdditionalEnergyDamage();
    }

    private void OnDestroy()
    {
        if (_rune != null) _rune.OnRuneSpent -= HandleRuneSpent;
        UnsubscribeForAdditionalEnergyDamage();
    }

    private void AddResourceTypeRune()
    {
        if (Hero.TryGetResource(ResourceType.Rune, out var resource))
        {
            _rune = resource as RuneComponent;
            if (_rune != null) _rune.OnRuneSpent += HandleRuneSpent;
        }
    }
    
    private void SubscribeForAdditionalEnergyDamage()
    {
        foreach (var energySkill in _hero.Abilities.Abilities)
        {
            if (energySkill is IEnergyDamagable { IsFrostEnergyApplied: true })
            {
                energySkill.OnBeforeApplyDamage += ModifyFrostEnergyBonus;
            }

            if (energySkill is IEnergyDamagable)
            {
                if (energySkill is PhysicalAttack || energySkill is IceShard)
                {
                    energySkill.OnBeforeApplyDamage += TryApplyFrostEnergyCooling;
                }
            }
        }
    }
    
    private void UnsubscribeForAdditionalEnergyDamage()
    {
        foreach (var energySkill in _hero.Abilities.Abilities)
        {
            if (energySkill is IEnergyDamagable { IsFrostEnergyApplied: true })
            {
                energySkill.OnBeforeApplyDamage -= ModifyFrostEnergyBonus;
            }
            
            if (energySkill is IEnergyDamagable)
            {
                if (energySkill is PhysicalAttack || energySkill is IceShard)
                {
                    energySkill.OnBeforeApplyDamage -= TryApplyFrostEnergyCooling;
                }
            }
        }
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Hero);
        callbackDataSaved(targetInfo);
        yield break;
    }

    protected override IEnumerator CastJob()
    {
        if (Hero == null || Hero.CharacterState == null)
            yield break;

        SkillToggleFrostEnergyState(Hero.gameObject);
        yield break;
    }

    private void SkillToggleFrostEnergyState(GameObject targetObj)
    {
        if (targetObj == null) return;

        Character character = targetObj.GetComponent<Character>();
        if (character == null || character.CharacterState == null) return;

        if (character.CharacterState.CheckForState(States.FrostEnergy))
        {
            character.CharacterState.CmdRemoveState(States.FrostEnergy);
            StopDrain(character);
        }
        else
        {
            if (!Cost.TryPaySingle(_runeCost, ResourceType.Rune, shouldModify: false))
                return;

            StartCoroutine(ClearSelfActivatingFlag());
            _isSelfActivating = true;

            character.CharacterState.CmdAddState(States.FrostEnergy, 999f, 0f,
                Schools.Water, character.gameObject, name);
            StartDrain(character);
        }
    }

    private IEnumerator ClearSelfActivatingFlag()
    {
        yield return new WaitForSeconds(SelfCastingThreshold);
        _isSelfActivating = false;
    }

    private void HandleRuneSpent(float value, Skill skill)
    {
        if (_isSelfActivating) return;
        if (!Hero.CharacterState.CheckForState(States.FrostEnergy)) return;
        if (isClient)
            SkillToggleFrostEnergyState(_hero.gameObject);
    }

    private void StartDrain(Character character)
    {
        if (_drainRoutine != null)
            StopCoroutine(_drainRoutine);

        _drainRoutine = StartCoroutine(DrainRoutine(character));
    }

    private void StopDrain(Character character)
    {
        if (_drainRoutine != null)
        {
            StopCoroutine(_drainRoutine);
            _drainRoutine = null;
        }
    }

    private IEnumerator DrainRoutine(Character character)
    {
        yield return new WaitForSeconds(StartDelay);

        while (character != null && character.CharacterState.CheckForState(States.FrostEnergy))
        {
            if (!Cost.TryPaySingle(EnergyPerTick, ResourceType.Energy, shouldModify: true))
            {
                character.CharacterState.CmdRemoveState(States.FrostEnergy);
                break;
            }

            yield return new WaitForSeconds(DrainInterval);
        }

        _drainRoutine = null;
    }

    private void ApplyEnergyBonusEffect(float spentRune)
    {
        if (_rune == null) return;

        float bonusEnergy = spentRune * 0.4f;

        if (Hero.TryGetResource(ResourceType.Energy, out var resource))
        {
            Energy energy = resource as Energy;
            energy?.CmdAdd(bonusEnergy);
            energy?.ForceRegenNow();
        }
    }

    private void ModifyFrostEnergyBonus(ref Damage damage, Skill skill, GameObject target)
    {
        if (!HeroHasFrostEnergy) return;
        if (target == null) return;
    
        var character = target.GetComponent<Character>();
        if (character == null) return;
 
        int coolingStacks = character.CharacterState.CheckStateStacks(States.Cooling);
        if (coolingStacks > 0)
            damage.Value += coolingStacks * FrostEnergyCoolingBonusPerStack;

        if (character.CharacterState.CheckForState(States.Frosting))
            damage.Value += FrostEnergyFrostingBonus;

        if (character.CharacterState.CheckForState(States.Frozen))
            damage.Value += FrostEnergyFrozenBonus;
    }
    
    public void ApplyFrostEnergyStateBonus(Character target, States appliedState, Skill sourceSkill)
    {
        if (!HeroHasFrostEnergy) return;
        if (target == null || sourceSkill == null) return;

        float bonusDamage = 0f;

        switch (appliedState)
        {
            case States.Cooling:
                int stacksAfterApply = target.CharacterState.CheckStateStacks(States.Cooling) + 1;
                bonusDamage = stacksAfterApply * FrostEnergyCoolingBonusPerStack;
                break;

            case States.Frosting:
                bonusDamage = FrostEnergyFrostingBonus;
                break;

            case States.Frozen:
                bonusDamage = FrostEnergyFrozenBonus;
                break;
        }

        if (bonusDamage <= 0f) return;

        Damage bonus = new Damage { Value = bonusDamage, Type = DamageType.Magical };
        sourceSkill.ApplyDamage(bonus, target.gameObject);
    }
    
    private void TryApplyFrostEnergyCooling(ref Damage damage, Skill skill, GameObject target)
    {
        if (!HeroHasFrostEnergy) return;
        if (!isServer) return;
        if (target == null) return;

        var character = target.GetComponent<Character>();
        if (character == null) return;

        if (UnityEngine.Random.Range(0f, 100f) <= FrostEnergyPhysicalCoolingChance)
        {
            character.CharacterState.AddState(States.Cooling, 12f, 0f,Schools.Water , Hero.gameObject, skill.Name);
        }
    }
}