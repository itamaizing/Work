using System;
using System.Collections;
using UnityEngine;

public class Dark1PassiveSkill : Skill, IPassiveSkill
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

    private void OnEnable()
    {
        Hero.DamageTracker.OnDamageTracked += OnDamageMade;
        Hero.DamageTracker.OnHealTracked += OnHealMade;
    }

    private void OnDisable()
    {
        Hero.DamageTracker.OnDamageTracked -= OnDamageMade;
        Hero.DamageTracker.OnHealTracked -= OnHealMade;
    }

    private void OnDamageMade(Damage damage, GameObject attacker)
    {
        if (damage.Value > 0 && damage.School == Schools.Dark)
        {
            Heal heal = new Heal();
            heal.Value = damage.Value * .1f;

            Hero.Heal(ref heal, this.Name, this);
        }
    }

    private void OnHealMade(Heal healed)
    {
        if (healed.Value > 0 && healed.DamageableSkill.School == Schools.Light)
        {
            Heal heal = new Heal();
            heal.Value = healed.Value * .1f;

            Hero.Heal(ref heal, this.Name, this);
        }
    }
}
