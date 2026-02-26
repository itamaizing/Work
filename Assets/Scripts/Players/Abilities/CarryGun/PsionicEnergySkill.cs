using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PsionicEnergySkill : Skill, IPassiveSkill
{
    #region Skill
    protected override int AnimTriggerCastDelay => throw new NotImplementedException();
    protected override int AnimTriggerCast => throw new NotImplementedException();
    public override void LoadTargetData(TargetInfo targetInfo) => throw new NotImplementedException();

    protected override IEnumerator CastJob()
    {
        yield return null;
    }

    protected override void ClearData() => throw new NotImplementedException();
    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback) => throw new NotImplementedException();
    #endregion

    [SerializeField] private BasePsionicEnergy basePsionicEnergy;
    [SerializeField] private float modifier = 1f;

    private const float PsiExplosionPercent = 0.3f;

    #region Talent
    private bool _isPsiEnergyActive = false;
    private bool _isDischargingPsiTalent = false;

    public bool IsDischargingPsiTalent { get => _isDischargingPsiTalent; set => _isDischargingPsiTalent = value; }
    public bool IsPsiEnergyActive { get => _isPsiEnergyActive; set => _isPsiEnergyActive = value;}

    public void DischargingPsiTalen(bool value) => _isDischargingPsiTalent = value;
    public void PsiEnergyActive(bool value) => _isPsiEnergyActive = value;
    #endregion

    public void HandleIncomingDamage(ref Damage damage, Skill skill)
    {
        if (!_isPsiEnergyActive && !_isDischargingPsiTalent) return;
        if (damage.Value <= 0 || basePsionicEnergy.CurrentValue <= 0) return;

        float absorptionAmount = Mathf.Min(basePsionicEnergy.CurrentValue, damage.Value);
        basePsionicEnergy.UsePsiEnergy(absorptionAmount);

        float reduced = absorptionAmount * modifier;
        damage.Value -= reduced;
        damage.Value = Mathf.Max(damage.Value, 0f);

        float aoeDamageValue = absorptionAmount * PsiExplosionPercent;

        float radius = Radius;

        if (Hero.CharacterState.CheckForState(States.PsionicGeneration))
        {
            radius *= 2f;
        }

        var allCharacters = FindObjectsOfType<Character>();

        foreach (var target in allCharacters)
        {
            if (target == null) continue;
            if (target == Hero) continue;
            if (target.IsDead) continue;

            float sqrDistance = (target.transform.position - Hero.transform.position).sqrMagnitude;

            if (sqrDistance > radius * radius) continue;

            Damage aoeDamage = new Damage
            {
                Value = aoeDamageValue,
                Type = DamageType.Magical,
                School = Schools.Air,
                Form = AbilityForm.Magic
            };

            ApplyDamage(aoeDamage, target.gameObject);
        }
    }
}
