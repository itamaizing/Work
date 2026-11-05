using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class IgnitionStateStacking : StateStackingRefreshing, ITickableState
{
    private const float BaseScorchedChance = 0.05f;
    private const float ScorchedDuration = 6f;

    private readonly List<StatusEffect> _effects = new() { StatusEffect.Others };

    private int _currentTick;
    private float _damageBonus;
    private float _fireBreathBonus;

    public override States State => States.Ignition;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override Schools Schools => Schools.Fire;
    public override List<StatusEffect> Effects => _effects;

    public float TickInterval => 1f;

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _currentTick = 0;
        _damageBonus = ExtractComboBonus(skillName);
        _fireBreathBonus = 0f;
        
        skill = personWhoMadeBuff?.Abilities?.GetSkill<IgnitionSkill>();

        MaxDuration = durationToExit;
        RemainingDuration = durationToExit;
        CurrentStacksCount = 1;

        ResetTickTimer();
    }

    public override void Reapply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        Apply(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
    }

    public void Tick()
    {
        if (characterState == null || !characterState.isServer || health == null) return;

        _currentTick++;

        float finalDamage = (_currentTick + _damageBonus) * (1f + _fireBreathBonus);

        var damage = new Damage
        {
            Value = finalDamage,
            Type = DamageType.Magical,
            School = Schools.Fire,
            IsDot = true,
            DamageKey = "ignition"
        };

        health.TryTakeDamage(ref damage, skill);

        if (sourceCaster == null)
            return;

        float chance = BaseScorchedChance * _currentTick;

        if (skill != null)
            chance = skill.Attributes.GetChance(chance);

        if (Random.value < Mathf.Clamp01(chance))
        {
            characterState.AddState(States.ScorchedSoul, ScorchedDuration, 0f, Schools.Fire, sourceCaster.gameObject, nameof(IgnitionSkill));
        }
    }

    public void UpdateFireBreathBonus(float bonus)
    {
        _fireBreathBonus = Mathf.Max(0f, bonus);
    }

    protected override void OnExit()
    {
        _currentTick = 0;
        _damageBonus = 0f;
        _fireBreathBonus = 0f;
        CurrentStacksCount = 0;
        skill = null;

        ResetTickTimer();
    }

    private static float ExtractComboBonus(string skillName)
    {
        const string prefix = nameof(IgnitionSkill);

        if (string.IsNullOrEmpty(skillName) || !skillName.StartsWith(prefix))
        {
            return 0f;
        }

        string value = skillName.Substring(prefix.Length);

        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float bonus) 
            || float.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out bonus))
        {
            return Mathf.Max(0f, bonus);
        }

        return 0f;
    }
}