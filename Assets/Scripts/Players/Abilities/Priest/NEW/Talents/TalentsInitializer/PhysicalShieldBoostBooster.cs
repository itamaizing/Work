using Mirror;
using UnityEngine;

public class PhysicalShieldBoostBooster : SkillTalentHandler
{
    private bool _enabled;

    private const float MaxPhysicalBoostPercentage = 0.5f;
    private const float PhysicalBoostPerUnit = 1f;
    private const float PhysicUnit = 10f;
    private const float PhysBoostTimeWindow = 5f;

    private float _physDamageAccumulator = 0f;
    private float _lastPhysDamageTime = -999f;

    public PhysicalShieldBoostBooster(NetworkBehaviour owner) : base(owner) { }

    public void Enable(bool value) => _enabled = value;

    public void OnPhysicalDamageTaken(Damage damage)
    {
        if (!_enabled || damage.Value <= 0f || damage.School != Schools.Physical)
            return;

        if (Time.time - _lastPhysDamageTime > PhysBoostTimeWindow)
            _physDamageAccumulator = 0f;

        _physDamageAccumulator += damage.Value;
        _lastPhysDamageTime = Time.time;

        var priestShield = Owner.GetComponent<PriestShield>();
        if (priestShield != null)
        {
            float boost = CalculateBoost();
            priestShield.SetPhysicalShieldBoostValue(boost);
        }
    }

    private float CalculateBoost()
    {
        return Mathf.Floor(_physDamageAccumulator / PhysicUnit) * PhysicalBoostPerUnit;
    }

    public void ResetAccumulator()
    {
        _physDamageAccumulator = 0f;
        _lastPhysDamageTime = -999f;
    }
}