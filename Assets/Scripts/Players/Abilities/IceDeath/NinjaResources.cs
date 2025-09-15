using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class NinjaResources : Skill, IPassiveSkill
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

    private bool _isIceRuneTalent;

    private void OnEnable()
    {
        if (Hero != null && Hero.DamageTracker != null)
        {
            Hero.DamageTracker.OnDamageTracked += OnDamageTaken;
        }
    }

    private void OnDisable()
    {
        if (Hero != null && Hero.DamageTracker != null)
        {
            Hero.DamageTracker.OnDamageTracked -= OnDamageTaken;
        }
    }

    private void OnDamageTaken(Damage damage, GameObject attacker)
    {
        if (_isIceRuneTalent && damage.Value > 0  && Hero.TryGetResource(ResourceType.Energy) is Energy energy)
        {
            float energyToRestore = damage.Value * 0.2f;
            energy.Add(energyToRestore);
        }
    }

    public void EnergyToRestore(bool value, string text)
    {
        _isIceRuneTalent = value;
        AbilityInfoHero.FinalDescription = value ? AbilityInfoHero.Description + $" {text}" : AbilityInfoHero.Description;
    }
}
