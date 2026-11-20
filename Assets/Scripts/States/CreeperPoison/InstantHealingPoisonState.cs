using System.Collections.Generic;

public class InstantHealingPoisonState : StateBasic
{
    private const float BaseHealingValue = 14f;
    private readonly List<StatusEffect> _effects = new() { StatusEffect.Healing, StatusEffect.Poison };

    public override bool IsUnique => false;
    public override States State => States.InstantHealingPoison;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public override void Apply(CharacterState character, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
    }

    public override void UpdateState()
    {
        if (characterState == null || !characterState.isServer) return;

        HealingPoisonPerSecondState latestPoison = null;
        foreach (var state in characterState.CurrentStates)
        {
            if (state is HealingPoisonPerSecondState poison &&
                (latestPoison == null || poison.ApplicationOrder > latestPoison.ApplicationOrder))
                latestPoison = poison;
        }

        Heal heal = new Heal
        {
            Value = BaseHealingValue + (latestPoison?.TotalHealValue ?? 0f),
            DamageableSkill = null
        };
        characterState.Character.Health.Heal(ref heal, null);
        ExitState();
    }
}