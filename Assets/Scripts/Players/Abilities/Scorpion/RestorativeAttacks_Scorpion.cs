using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class RestorativeAttacks_Scorpion : Skill
{
    protected override int AnimTriggerPrepare => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => CheckCanCast() && Charges.HasCharges;
    
    private float _accumulatedPhysDamage = 0f;
    
    private float _physDamageThreshold   = 50f;
    
    private bool CheckCanCast()
    {
        if (Hero.CharacterState.CheckForState(States.RestorativeAttacks)) return false;
        return true;
    }

    protected override void Awake()
    {
        base.Awake();
    }

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);

        foreach (var skill in Hero.Abilities.Abilities)
        {
            if (skill is IComboParticipatingSkill combo)
            {
                combo.OnDamaged += OnAttackApplied;
            }
        }
        hero.Health.DamageTaken += TrackPhysDamage;
        UpdateDisactiveFromCharges();
    }

    private void OnDisable()
    {
        if (Hero == null) return;
        foreach (var skill in Hero.Abilities.Abilities)
        {
            if (skill is IComboParticipatingSkill combo)
                combo.OnDamaged -= OnAttackApplied;
        }
        _hero.Health.DamageTaken -= TrackPhysDamage;
    }
    
    private void TrackPhysDamage(Damage damage, Skill skill)
    {
        if(!isOwned) return;
        if (damage.Type != DamageType.Physical) return;

        _accumulatedPhysDamage += damage.Value;

        while (_accumulatedPhysDamage >= _physDamageThreshold)
        {
            _accumulatedPhysDamage -= _physDamageThreshold;
            AddCharge();

            if (Charges.RemainingCharges > 0)
            {
                Disactive = false;
            }
        }
    }
    
    private void AddCharge()
    {
        if (Charges.RemainingCharges >= Charges.MaxCharges) return;
        if (Charges.RechargeTimers.Count > 0)
            Charges.RestoreCharge(0);
        UpdateDisactiveFromCharges();
    }

    private void UpdateDisactiveFromCharges()
    {
        Disactive = !Charges.HasCharges;
        Charges.SendCurrentChange(Charges.RemainingCharges);
    }

    protected override void UseCooldownOrCharges()
    {
        Charges.TryUse();

        UpdateDisactiveFromCharges();
    }
    
    private void OnAttackApplied(GameObject target, Skill sourceSkill)
    {
        if (Hero.CharacterState.CheckForState(States.RestorativeAttacks))
        {
            var state = Hero.CharacterState.GetState(States.RestorativeAttacks) as RestorativeAttacksState;
            state?.OnAttackHit(sourceSkill);
            RpcOnAttackApplied(sourceSkill);
        }
    }

    [ClientRpc]
    private void RpcOnAttackApplied(Skill sourceSkill)
    {
        if (Hero.CharacterState.CheckForState(States.RestorativeAttacks))
        {
            var state = Hero.CharacterState.GetState(States.RestorativeAttacks) as RestorativeAttacksState;
            state?.OnAttackHit(sourceSkill);
        }
    }

    protected override IEnumerator TargetingJob(Action<TargetInfo> callbackDataSaved)
    {
        callbackDataSaved(new TargetInfo());
        yield return null;
    }

    protected override IEnumerator CastJob()
    {
        //CommitUse();
        
        if(isClient)
            Hero.CharacterState.CmdAddState(States.RestorativeAttacks, 3f, 0f, Schools.None, Hero.gameObject, nameof(RestorativeAttacks_Scorpion));

        yield return null;
    }

    protected override void ClearData() { }

    public override void LoadTargetData(TargetInfo targetInfo) { }
}