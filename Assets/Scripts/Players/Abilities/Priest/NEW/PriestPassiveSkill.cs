using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PriestPassiveSkill : Skill, IPassiveSkill
{
    #region Skill
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => false;
    protected override IEnumerator CastJob() => null;
    protected override void ClearData() { }
    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved) => null;
    public override void LoadTargetData(TargetInfo targetInfo) => throw new NotImplementedException();
    #endregion

    #region Talent
    private bool _isDamageDarkLightStun;
    private bool _isDamageDarkHealLightAddHealth;

    public void DamageDarkLightStun(bool value)
    {
        _isDamageDarkLightStun = value;
    }

    public void DamageDarkHealLightAddHealth(bool value)
    {
        _isDamageDarkHealLightAddHealth = value;
    }
    #endregion

    private void OnEnable()
    {
        if (Hero?.DamageTracker != null)
        {
            Hero.DamageTracker.OnDamageTracked += HandleDamageDealt;
            Hero.DamageTracker.OnHealTracked += HandleHealDone;
        }
    }
    private void OnDisable()
    {
        if (Hero?.DamageTracker != null)
        {
            Hero.DamageTracker.OnDamageTracked -= HandleDamageDealt;
            Hero.DamageTracker.OnHealTracked -= HandleHealDone;
        }
    }

    private void HandleDamageDealt(Damage damage, GameObject targetObject)
    {
        if (!_isDamageDarkLightStun) return;
        if (damage.School != Schools.Light && damage.School != Schools.Dark) return;

        if (targetObject.TryGetComponent<Character>(out var target))
        {
            float chance = UnityEngine.Random.Range(0f, 100f);
            if (chance <= 30f)
            {
                float stunDuration = damage.Value * 0.1f;

                target.CharacterState.AddState(States.Stun, stunDuration, 0, Hero.gameObject, nameof(PriestPassiveSkill));
            }
        }

        if (_isDamageDarkHealLightAddHealth && damage.School == Schools.Dark)
        {
            float healAmount = damage.Value * 4f;

            var extraHeal = new Heal
            {
                Value = healAmount,
                DamageableSkill = this
            };

            ApplyHeal(extraHeal, Hero.gameObject, this, nameof(PriestPassiveSkill));
            Debug.Log($"[PriestPassive] Restored {healAmount} HP from Dark damage ({damage.Value})");
        }
    }

    private void HandleHealDone(Heal heal)
    {
        if (!_isDamageDarkHealLightAddHealth) return;
        if (heal.DamageableSkill == null) return;
        if (heal.DamageableSkill.School != Schools.Light) return;

        float healAmount = heal.Value * 4f;

        var extraHeal = new Heal
        {
            Value = healAmount,
            DamageableSkill = this
        };

        ApplyHeal(extraHeal, Hero.gameObject, this, nameof(PriestPassiveSkill));

        Debug.Log($"[PriestPassive] Restored {healAmount} HP from Light healing ({heal.Value})");
    }
}
