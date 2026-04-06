using Mirror;
using UnityEngine;

public class HealingBoostBooster : SkillTalentHandler
{
    private bool _enabled;
    
    private const float HealingBoostPerUnit = 1f;
    private const float HealingUnit = 10f;

    private float _healingAccumulator = 0f;
    private float _lastHealingTime = -999f;
    private const float HealingResetTime = 5f;

    public HealingBoostBooster(NetworkBehaviour owner) : base(owner) { }

    public override void Enable(bool value) => _enabled = value;

    public void OnHealDone(Heal heal)
    {
        if (!_enabled || heal.Value <= 0f || heal.DamageableSkill == null) 
            return;

        // Только Light-школа считается для этого таланта
        if (heal.DamageableSkill.Info.School != Schools.Light) 
            return;

        if (Time.time - _lastHealingTime > HealingResetTime)
            _healingAccumulator = 0f;

        _healingAccumulator += heal.Value;
        _lastHealingTime = Time.time;

        var priestShield = Owner.GetComponent<PriestShield>();
        if (priestShield != null)
        {
            float boost = CalculateBoost();
            priestShield.SetHealingBoostValue(boost);
        }
    }

    private float CalculateBoost()
    {
        return Mathf.Floor(_healingAccumulator / HealingUnit) * HealingBoostPerUnit;
    }

    public void ResetAccumulator()
    {
        _healingAccumulator = 0f;
        _lastHealingTime = -999f;
    }
}
