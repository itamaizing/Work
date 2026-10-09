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
    
    private const float PsiExplosionPercent = 0.3f;
    private const float PsiExplosionRadius = 3f;
    private const float ProtectiveAbsorbRatio = 0.5f;

    private readonly List<StatusEffect> _effects = new() { StatusEffect.Ability };

    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State => States.Impatience;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    private bool _subscribed;
    
    public override bool IsUnique => false;

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        if (!character.isServer) return;

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
        
        if (_isAccumulationActive
            && _casterPsionic != null
            && damage.Type == DamageType.Physical
            && _casterPsionic.IsAttackingPsiEnergyActive)
        {
            _casterPsionic.AddPsiAndRestartDecay(originalDamage);
        }

        if (_impatica != null
            && _impatica.IsExtendDamageAbsorption
            && _casterPsionic != null
            && _casterPsionic.CurrentValue > 0f)
        {
            float absorbAmount = Mathf.Min(_casterPsionic.CurrentValue, damage.Value);
            _casterPsionic.UsePsiEnergy(absorbAmount);

            damage.Value -= absorbAmount * ProtectiveAbsorbRatio;
            damage.Value = Mathf.Max(damage.Value, 0f);

            float aoeDamageValue = absorbAmount * PsiExplosionPercent;
            if (aoeDamageValue > 0f)
            {
                int enemiesHitCount = 0;
                var hits = Physics.OverlapSphere(
                    characterState.Character.transform.position,
                    PsiExplosionRadius);

                foreach (var hit in hits)
                {
                    if (!hit.TryGetComponent<Character>(out var target)) continue;
                    if (target == characterState.Character || target.IsDead) continue;

                    var aoeDamage = new Damage
                    {
                        Value = aoeDamageValue,
                        Type = DamageType.Magical,
                        School = Schools.Air,
                        Form = AbilityForm.Magic
                    };

                    target.Health.TryTakeDamage(ref aoeDamage, skill);
                    enemiesHitCount++;
                }

                var psionicEnergy = _casterPsionic.PsionicEnergySkill;
                if (psionicEnergy != null && psionicEnergy.IsExtendedDuration && enemiesHitCount > 0)
                {
                    float bonusTime = enemiesHitCount * 0.1f;

                    var attacking = _casterPsionic.AttackingPsionicEnergy;
                    if (attacking != null)
                        attacking.ExtendDuration(bonusTime);

                    foreach (var character in ActiveCharacters)
                    {
                        if (character == null) continue;
                        if (character.CharacterState.GetState(States.Impatience)
                            is ImpatienceStateStacking state)
                        {
                            state.ExtendDuration(bonusTime);
                        }
                    }
                }
            }
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
                PhysicAttackType = damage.PhysicAttackType,
                SkillType = damage.SkillType,
                SourceSkill = damage.SourceSkill,
                DamageKey = ShareKey,
                FullyAbsorbed = false
            };

            character.Health.TryTakeDamage(ref shared, skill);
        }

        damage.Value = divided;
    }
    
    [Server]
    public void ExtendDuration(float amount)
    {
        if (amount <= 0f) return;
        IncreaseDuration(amount);
    }
}