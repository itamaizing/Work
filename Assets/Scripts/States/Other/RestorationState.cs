using System.Collections.Generic;
using Mirror;

public class RestorationStateStacking : RefreshingStateStacking, ITickableState
{
    private const float _tickInterval = 3f;
    private const float _healPerTickBase = 6f;

    private Character _targetCharacter;

    private readonly List<StatusEffect> _effects = new() { StatusEffect.Restoration };

    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State { get; }
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    public float TickInterval => _tickInterval;

    public RestorationStateStacking(States stateType) => State = stateType;
    public RestorationStateStacking() { }

    private bool IsStackingMode => State == States.RestorationStacking;

    public override void Apply(CharacterState character, float durationToExit, float damageToExit,
        Character sourceCaster, string skillName)
    {
        SetMaxStacks(IsStackingMode ? 2 : 1);
        SetMaxStacks(IsStackingMode ? 2 : 1);
        currentStacksCount = 1;

        sourceCaster?.Abilities?.GetSkill<Restoration>()?.RestorationHealBooster.Reset();

        if (_targetCharacter == null)
        {
            _targetCharacter = character.Character;
            _targetCharacter.Health.HealTakedServer += OnTargetHealTaken;
        }

        Tick();
    }

    public override void UpdateState() { }

    public void Tick() => ApplyHealTick();

    private void ApplyHealTick()
    {
        float baseHeal = _healPerTickBase * currentStacksCount;
        float healValue = baseHeal + GetSpiritEnergyBonus(characterState.Character);

        var spark = sourceCaster?.Abilities?.GetSkill<SparkOfLight>();
        spark?.OverhealManaBooster.OnAnyHealTaken(characterState.Character, healValue, spark);

        var restoration = sourceCaster?.Abilities?.GetSkill<Restoration>();
        restoration?.RestorationManaBooster.OnRestorationTick(healValue, characterState.Character);
        healValue += restoration?.RestorationHealBooster.BonusHeal ?? 0f;

        CmdHeal(healValue);
    }

    private float GetSpiritEnergyBonus(Character character) =>
        (character?.CharacterState?.GetState(States.SpiritEnergy) as SpiritEnergyStateStacking)?.GetHealBonus() ?? 0f;

    public override bool Stack(float time)
    {
        if (IsStackingMode && currentStacksCount < MaxStacksCount)
            currentStacksCount++;

        RemainingDuration = time;
        UpdateDisplayText();

        return true;
    }

    public override void ReduceStack()
    {
        if (currentStacksCount <= 1)
        {
            ExitState();
            return;
        }

        currentStacksCount--;
        RemainingDuration = MaxDuration;
        UpdateDisplayText();
    }

    public override void ExitState()
    {
        if (_targetCharacter != null)
        {
            _targetCharacter.Health.HealTakedServer -= OnTargetHealTaken;
            _targetCharacter = null;
        }

        characterState.RemoveState(this);
    }

    [Server]
    private void CmdHeal(float healValue) => ClientRpcHeal(healValue);

    [ClientRpc]
    private void ClientRpcHeal(float healValue)
    {
        Heal heal = new() { Value = healValue, DamageableSkill = null };
        health.Heal(ref heal, nameof(RestorationStateStacking), null);
    }

    private void OnTargetHealTaken(float healValue, Skill sourceSkill, string sourceName)
    {
        if (sourceSkill == null || healValue <= 0f) return;
        if (sourceSkill.Hero != sourceCaster) return;

        sourceCaster?.Abilities?.GetSkill<Restoration>()?.RestorationHealBooster.OnHealReceived(healValue);
    }
}