using Mirror;
using UnityEngine;

public class DarkMagicBoostBooster : SkillTalentHandler
{
    private bool _enabled;
    
    private const float DarkMagicBoostPerUnit = 1f;
    private const float DarkMagicUnit = 10f;
    private const float DarkDamageResetTime = 5f;

    private float _accumulatedDarkDamage = 0f;
    private float _lastDarkDamageTime = -999f;

    public DarkMagicBoostBooster(NetworkBehaviour owner) : base(owner) { }

    public override void Enable(bool value) => _enabled = value;
    
    public void OnDarkDamageDone(Damage damage)
    {
        if (!_enabled || damage.Value <= 0f || damage.School != Schools.Dark)
            return;

        if (Time.time - _lastDarkDamageTime > DarkDamageResetTime)
            _accumulatedDarkDamage = 0f;

        _accumulatedDarkDamage += damage.Value;
        _lastDarkDamageTime = Time.time;

        var priestShield = Owner.GetComponent<PriestShield>();
        if (priestShield != null)
        {
            float boost = CalculateBoost();
            priestShield.SetDarkMagicBoostValue(boost);
        }
    }

    private float CalculateBoost()
    {
        return Mathf.Floor(_accumulatedDarkDamage / DarkMagicUnit) * DarkMagicBoostPerUnit;
    }

    public void ResetAccumulator()
    {
        _accumulatedDarkDamage = 0f;
        _lastDarkDamageTime = -999f;
    }
}
