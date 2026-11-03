using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class ImpatienceStateStacking : StateStackingRefreshing
{
    private static readonly HashSet<Character> ActiveCharacters = new();

    private bool _isAccumulationActive;
    private BasePsionicEnergy _casterPsionic;
    private Impatica _impatica;

    private const string ShareKey = "ImpatienceShare";

    private readonly List<StatusEffect> _effects = new() { StatusEffect.Ability };

    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State => States.Impatience;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;
    
    private static readonly HashSet<ImpatienceStateStacking> ActiveStates = new();

    private double _clientEndTime;

    public uint NetworkId { get; private set; }

    public override bool CanExceedMaxDuration => true;

    private bool _subscribed;
    
    public override bool IsUnique => false;

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        if (!character.isServer) return;
        
        ActiveStates.Add(this);
        ActiveCharacters.Add(character.Character);

        if (!_subscribed && health != null)
        {
            health.OnBeforeDamage += HandleBeforeDamage;
            _subscribed = true;
        }

        if (personWhoMadeBuff == null) return;

        _casterPsionic = personWhoMadeBuff.GetComponent<BasePsionicEnergy>();
        _impatica = personWhoMadeBuff.Abilities != null
            ? personWhoMadeBuff.Abilities.GetSkill<Impatica>()
            : null;

        if (_casterPsionic != null)
        {
            _isAccumulationActive = _casterPsionic.IsPsionicsTalentActive;
            _casterPsionic.OnAccumulationPsionicChanged += HandleAccumulationChanged;
        }
    }

    public override bool Stack(float time)
    {
        RemainingDuration = time;
        if (time > MaxDuration)
            MaxDuration = time;
        return false;
    }

    protected override void OnExit()
    {
        ActiveStates.Remove(this);

        if (characterState != null
            && characterState.isServer
            && NetworkId != 0
            && _impatica != null)
        {
            _impatica.SendImpaticaRemoval(this);
        }
        
        if (characterState != null && characterState.Character != null && characterState.Character.isServer)
        {
            ActiveCharacters.Remove(characterState.Character);

            if (_subscribed && health != null)
            {
                health.OnBeforeDamage -= HandleBeforeDamage;
                _subscribed = false;
            }

            if (_casterPsionic != null)
                _casterPsionic.OnAccumulationPsionicChanged -= HandleAccumulationChanged;
        }

        _casterPsionic = null;
        _impatica = null;
    }

    private void HandleAccumulationChanged(bool value) => _isAccumulationActive = value;

    private void HandleBeforeDamage(ref Damage damage, Skill skill)
    {
        if (characterState == null || !characterState.isServer) return;

        if (damage.DamageKey == ShareKey) return;

        float originalDamage = damage.Value;

        if (damage.Value <= 0f) return;
        
        if (_isAccumulationActive && _casterPsionic != null && damage.Type == DamageType.Physical && _casterPsionic.IsAttackingPsiEnergyActive)
        {
            _casterPsionic.AddPsiAndRestartDecay(originalDamage);
        }

        if (_impatica != null && _impatica.IsExtendDamageAbsorption && _casterPsionic != null)
        {
            _casterPsionic.TryTakeDamage(ref damage, skill, characterState.Character.transform.position);
        }

        if (damage.Value <= 0f) return;

        var recipients = new List<Character>(ActiveCharacters);

        if (sourceCaster != null &&
            !sourceCaster.IsDead &&
            !recipients.Contains(sourceCaster))
        {
            recipients.Add(sourceCaster);
        }

        if (recipients.Count <= 1) return;

        float divided = damage.Value / recipients.Count;

        foreach (var character in recipients)
        {
            if (character == null || character.IsDead) continue;
            if (character == characterState.Character) continue;

            var shared = new Damage
            {
                Value = divided,
                Type = damage.Type,
                School = damage.School,
                Form = damage.Form,
                AttackRange = damage.AttackRange,
                TargetingType = damage.TargetingType,
                SourceSkill = damage.SourceSkill,
                DamageKey = ShareKey,
                FullyAbsorbed = false
            };

            character.Health.TryTakeDamage(ref shared, skill);
        }

        damage.Value = divided;
    }
    
    public void ApplyNetworkSnapshot(Impatica owner, uint networkId, double endTime, float maxDuration)
    {
        _impatica = owner;
        NetworkId = networkId;
        _clientEndTime = endTime;

        MaxDuration = maxDuration;
        SetDuration(Mathf.Max(0f, (float)(endTime - NetworkTime.time)));
    }

    public override void GlobalUpdate()
    {
        if (characterState == null) return;

        if (characterState.isServer)
        {
            base.GlobalUpdate();
            return;
        }
        
        SetDuration(Mathf.Max(0f, (float)(_clientEndTime - NetworkTime.time)));
    }

    public static void ExtendForCaster(Character caster, float amount)
    {
        if (!NetworkServer.active || caster == null || amount <= 0f)
            return;

        foreach (var state in new List<ImpatienceStateStacking>(ActiveStates))
        {
            if (state.SourceCaster != caster)
                continue;

            state.ExtendDuration(amount);
        }
    }
    
    public void ExtendDuration(float amount)
    {
        if (characterState == null || !characterState.isServer)
            return;

        if (amount <= 0f || RemainingDuration <= 0f)
            return;

        IncreaseDuration(amount);
        _impatica?.SendImpaticaSnapshot(this);
    }
}