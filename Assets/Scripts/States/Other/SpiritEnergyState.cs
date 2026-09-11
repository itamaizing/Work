using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class SpiritEnergyStateStacking : RefreshingStateStacking
{
    private const float DamageManaRestorePercent = 0.05f;
    private const int _baseMaxStacks = 3;

    private float _baseDuration;
    private float _regenAmount;

    private GameObject _spiritEnergyStateEffectInstance;
    private Health _healthComponent;
    private Resource _manaResource;

    private List<StatusEffect> _effects = new() { StatusEffect.Healing };
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State => States.SpiritEnergy;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    public override void Apply(CharacterState character, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        _baseDuration = durationToExit;
        RemainingDuration = durationToExit;
        currentStacksCount = 1;
        SetMaxStacks(_baseMaxStacks);

        _healthComponent = characterState.Character.Health;
        _manaResource = characterState.Character.TryGetResource(ResourceType.Mana);

        if (_healthComponent != null)
            _healthComponent.DamageTaken += OnDamageTaken;

        if (characterState.StateEffects.SpiritEnergyEffect != null)
        {
            _spiritEnergyStateEffectInstance = characterState.StateEffects.SpiritEnergyEffect;
            _spiritEnergyStateEffectInstance.SetActive(true);
        }

        RecalcRegenAmount();
    }

    public override void UpdateState() { }

    public override bool Stack(float time)
    {
        if (currentStacksCount < MaxStacksCount)
        {
            currentStacksCount++;
            RemainingDuration = _baseDuration;
        }
        else
        {
            RemainingDuration = _baseDuration;
        }

        RecalcRegenAmount();
        return true;
    }

    public override void ExitState()
    {
        if (_healthComponent != null)
            _healthComponent.DamageTaken -= OnDamageTaken;

        if (_spiritEnergyStateEffectInstance != null)
            _spiritEnergyStateEffectInstance.SetActive(false);

        currentStacksCount = 0;
        RemainingDuration = 0f;
        _baseDuration = 0f;
        _regenAmount = 0f;
        _healthComponent = null;
        _manaResource = null;
        _spiritEnergyStateEffectInstance = null;

        characterState?.RemoveState(this);
        characterState = null;
    }

    private void OnDamageTaken(Damage damage, Skill skill)
    {
        float manaRestoreValue = damage.Value * DamageManaRestorePercent * currentStacksCount;
        ApplyRegen(manaRestoreValue);
    }

    public float GetHealBonus() => currentStacksCount * 1f;

    public void ApplyRegen(float manaRestoreValue)
    {
        if (_manaResource != null && manaRestoreValue > 0)
            _manaResource.CmdAdd(manaRestoreValue);
    }

    private void RecalcRegenAmount()
    {
        if (_manaResource != null)
            _regenAmount = _manaResource.MaxValue * DamageManaRestorePercent * currentStacksCount;
    }
}