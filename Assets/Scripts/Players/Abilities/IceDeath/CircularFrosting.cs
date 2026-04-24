using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircularFrosting : Skill
{
    [SerializeField] private Character _player;
    [SerializeField] private SeriesOfStrikes _seriesOfStrikes;

    private List<Character> _enemies = new();

    private float _baseDuration = 2f;
    private float _duration = 2f;

    private Energy _energy;
    private bool _talentFrostingFrozen;

    private const float FrostEnergyCoolingBonusPerStack = 1f;
    private const float FrostEnergyFrostingBonusPerStack = 5f;
    private const float FrostEnergyFrozenBonusPerStack = 10f;

    protected override bool IsCanCast => true;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Hero);
        callbackDataSaved(targetInfo);
        yield break;
    }

    protected override IEnumerator CastJob()
    {
        if (_energy == null) _energy = (Energy)Hero.Resources[ResourceType.Energy];
        if (_energy == null) yield break;

        FindEnemies();
        ExplosionFrosting();

        yield return null;
    }

    private void FindEnemies()
    {
        _enemies.Clear();

        Collider[] hits = Physics.OverlapSphere(transform.position, AreaInfo.Radius);

        foreach (var col in hits)
        {
            Character character = col.GetComponent<Character>();
            if (character != null && character != Hero && !_enemies.Contains(character)) _enemies.Add(character);
        }
    }

    private void ExplosionFrosting()
    {
        float usedEnergy;

        if (_energy.CurrentValue >= 30f)
        {
            usedEnergy = 30f;
            _duration = _baseDuration + 3f;
        }
        else
        {
            usedEnergy = _energy.CurrentValue;
            _duration = _baseDuration + usedEnergy / 10f;
        }

        if (usedEnergy <= 0f) return;

        _energy.CmdUse(usedEnergy);

        foreach (Character target in _enemies)
        {
            if (target == null) continue;
            CmdApplyFrosting(target, usedEnergy, _duration);
        }
    }

    [Command]
    private void CmdApplyFrosting(Character target, float usedEnergy, float duration)
    {
        if (target == null) return;

        if (_seriesOfStrikes != null) _seriesOfStrikes.MakeHit(target, Info.AbilityForm, 1, usedEnergy, 0);
        if (_talentFrostingFrozen && target.CharacterState.CheckForState(States.Frosting)) ApplyStateWithBonus(target, States.Frozen, duration);
        ApplyStateWithBonus(target, States.Frosting, duration);
    }

    private void ApplyStateWithBonus(Character target, States state, float duration)
    {
        if (target == null || target.CharacterState == null) return;

        bool hasFrostEnergy = target.CharacterState.CheckForState(States.FrostEnergy);

        int currentStacks = target.CharacterState.CheckStateStacks(state);

        int stacksAfter = currentStacks + 1;

        float bonusPerStack = 0f;

        switch (state)
        {
            case States.Cooling:
                bonusPerStack = FrostEnergyCoolingBonusPerStack;
                break;

            case States.Frosting:
                bonusPerStack = FrostEnergyFrostingBonusPerStack;
                break;

            case States.Frozen:
                bonusPerStack = FrostEnergyFrozenBonusPerStack;
                break;
        }

        if (hasFrostEnergy && bonusPerStack > 0f)
        {
            float bonusDamage = stacksAfter * bonusPerStack;

            Damage bonus = new Damage
            {
                Value = bonusDamage,
                Type = DamageType.Magical
            };

            target.Health.TryTakeDamage(ref bonus, this);
        }

        target.CharacterState.AddState(state, duration, 0, Hero.gameObject, name);
    }

    public void SetTalentFrostingFrozen(bool value)
    {
        _talentFrostingFrozen = value;
    }
    protected override void ClearData()
    {
        _enemies.Clear();
    }
}