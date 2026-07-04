using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class ImpatienceState : AbstractCharacterState
{
    private float _durationRemaining;

    private static readonly HashSet<Character> ActiveCharacters = new();
    private static bool _isProcessingSharedDamage;

    private bool _isAccumulationActive;
    private bool _extendDamageAbsorption;
    private BasePsionicEnergy _casterPsionic;
    private Impatica _impatica;

    private const float PsiExplosionPercent = 0.3f;
    private const float PsiExplosionRadius = 3f;

    private List<StatusEffect> _effects = new() { StatusEffect.Ability };

    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State => States.Impatience;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        health = character.Character.Health;
        this.personWhoMadeBuff = personWhoMadeBuff;

        _durationRemaining = durationToExit;

        if (!character.isServer) return;

        ActiveCharacters.Add(character.Character);
        health.OnBeforeDamage += HandleBeforeDamage;

        if (personWhoMadeBuff != null)
        {
            _casterPsionic = personWhoMadeBuff.GetComponent<BasePsionicEnergy>();
            _impatica = personWhoMadeBuff.GetComponent<Impatica>();

            if (_casterPsionic != null) _casterPsionic.OnAccumulationPsionicChanged += HandleAccumulationChanged;
        }
    }

    public override void UpdateState()
    {

    }

    public override void ExitState()
    {
        if (characterState.Character.isServer)
        {
            ActiveCharacters.Remove(characterState.Character);

            if (health != null) health.OnBeforeDamage -= HandleBeforeDamage;

            if (_casterPsionic != null) _casterPsionic.OnAccumulationPsionicChanged -= HandleAccumulationChanged;
        }

        characterState.RemoveState(this);
    }

    private void HandleAccumulationChanged(bool value) => _isAccumulationActive = value;

    private void HandleBeforeDamage(ref Damage damage, Skill skill)
    {
        if (!NetworkServer.active) return;
        if (_isProcessingSharedDamage) return;
        if (damage.Value <= 0) return;

        float originalDamage = damage.Value;

        if (_isAccumulationActive && _casterPsionic != null)
        {
            float psiGain = originalDamage;

            _casterPsionic.AddPsiAndRestartDecay(psiGain);
        }

        if (_extendDamageAbsorption && _casterPsionic != null)
        {
            if (_casterPsionic.CurrentValue > 0)
            {
                float absorbAmount = Mathf.Min(_casterPsionic.CurrentValue, damage.Value);

                _casterPsionic.UsePsiEnergy(absorbAmount);

                damage.Value -= absorbAmount;
                damage.Value = Mathf.Max(damage.Value, 0f);

                float aoeDamageValue = absorbAmount * PsiExplosionPercent;

                if (aoeDamageValue > 0f)
                {
                    Collider[] hits = Physics.OverlapSphere( characterState.Character.transform.position, PsiExplosionRadius);

                    int enemiesHitCount = 0;

                    foreach (var hit in hits)
                    {
                        Character target = hit.GetComponent<Character>();
                        if (target == null) continue;
                        if (target == characterState.Character) continue;
                        if (target.IsDead) continue;

                        Damage aoeDamage = new Damage
                        {
                            Value = aoeDamageValue,
                            Type = DamageType.Magical,
                            School = Schools.Air,
                            Form = AbilityForm.Magic
                        };

                        target.Health.TryTakeDamage(ref aoeDamage, skill);

                        enemiesHitCount++;
                    }

                    var psionicEnergy = _casterPsionic.GetComponent<PsionicEnergySkill>();

                    if (psionicEnergy.IsExtendedDuration && enemiesHitCount > 0)
                    {
                        float bonusTime = enemiesHitCount * 0.1f;

                        var attacking = _casterPsionic.GetComponent<AttackingPsionicEnergy>();
                        if (attacking != null)
                            attacking.ExtendDuration(bonusTime);

                        foreach (var character in ActiveCharacters)
                        {
                            var state = character.CharacterState.GetState(States.Impatience) as ImpatienceState;
                            if (state != null)
                                state.ExtendDuration(bonusTime);
                        }
                    }
                }
            }
        }

        if (damage.Value <= 0f) return;

        List<Character> recipients = new List<Character>(ActiveCharacters);

        if (personWhoMadeBuff != null &&
            !personWhoMadeBuff.IsDead &&
            !recipients.Contains(personWhoMadeBuff))
        {
            recipients.Add(personWhoMadeBuff);
        }

        if (recipients.Count <= 1)
            return;

        float dividedDamage = damage.Value / recipients.Count;

        _isProcessingSharedDamage = true;

        try
        {
            foreach (var character in recipients)
            {
                if (character == characterState.Character) continue;
                if (character == null || character.IsDead) continue;

                Damage sharedDamage = new Damage
                {
                    Value = dividedDamage,
                    Type = damage.Type,
                    School = damage.School,
                    Form = damage.Form,
                    PhysicAttackType = damage.PhysicAttackType
                };

                character.Health.TryTakeDamage(ref sharedDamage, skill);
            }

            damage.Value = dividedDamage;
        }

        finally
        {
            _isProcessingSharedDamage = false;
        }
    }

    [Server]
    public void ExtendDuration(float amount)
    {
        _durationRemaining += amount;
    }
}