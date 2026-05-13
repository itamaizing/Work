using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

public class RingOfFireAura : AuraStateHandler
{
    [SerializeField] private ParticleSystem _ringParticle;
    
    private const float TickInterval = 1f;
    private const float ScorchedChance = 5f;
    private const float SlowedRegenRate = 2f;

    public float _baseRadius = 4;

    private FireBreath_Scorpion _fireBreath;
    private Coroutine _tickCoroutine;
    private float _originalEnergyRegen;
    private Resource _energy;

    protected override void OnAuraEnabled()
    {
        if (_owner.Resources.TryGetValue(ResourceType.Energy, out _energy))
            _originalEnergyRegen = _energy.RegenerationValue;

        CmdInit(_owner.gameObject);
        _fireBreath = _fromSkill?.Hero?.Abilities?.GetSkill<FireBreath_Scorpion>();
        if (isOwned)
            _tickCoroutine = StartCoroutine(TickRoutine());
        
        UpdateParticleRadius();
        _ringParticle.gameObject.SetActive(true);
        _ringParticle?.Play();
    }

    [Command]
    private void CmdInit(GameObject owner)
    {
        _owner = owner.GetComponent<Character>();
        _energy = _owner.Resources[ResourceType.Energy];
    }

    public bool IsTargetInRing(Character target)
    {
        return target != null && _currentTargets.Contains(target);
    }
    
    public IEnumerable<Character> GetCurrentTargets()
    {
        return _currentTargets.ToArray();
    }

    protected override void OnAuraDisabled()
    {
        if (_tickCoroutine != null)
        {
            StopCoroutine(_tickCoroutine);
            _tickCoroutine = null;
        }

        _fireBreath?.ClearExposureTicks();
        SetBaseRadius();
        _ringParticle?.Stop();
        _ringParticle.gameObject.SetActive(false);
    }
    
    public void SetRadius(float newRadius)
    {
        _radius = _baseRadius + newRadius;
        UpdateParticleRadius();
    }

    private void SetBaseRadius()
    {
        _radius = _baseRadius;
    }
    
    private void UpdateParticleRadius()
    {
        if (_ringParticle == null) return;

        _ringParticle.transform.localScale = new Vector3(_radius, 1f, _radius);
    }

    private IEnumerator TickRoutine()
    {
        while (IsActive)
        {
            yield return new WaitForSeconds(TickInterval);

            if (_energy == null || _energy.CurrentValue < 1f)
            {
                ActivateAura(false);
                yield break;
            }

            _energy.CmdUse(1f);
            SlowRegenForOneEnergy();

            foreach (var target in _currentTargets.ToArray())
            {
                if (target == null || target.IsDead) continue;

                float multiplier = _fireBreath?.GetExposureMultiplier(
                    target.GetComponent<Health>()) ?? 1f;

                var damage = new Damage
                {
                    Value = Random.Range(1, 4) * multiplier,
                    Type = DamageType.Magical,
                    School = Schools.Fire
                };
                _fromSkill.CmdApplyDamage(damage, target.gameObject);
                CmdApplyScorched(target.gameObject, _owner.gameObject);
            }
        }
    }


    private void SlowRegenForOneEnergy()
    {
        StartCoroutine(SlowRegenJob());
    }

    private IEnumerator SlowRegenJob()
    {
        if (_energy == null) yield break;
        ;
        float savedRegen = _energy.RegenerationValue;
        _energy.RegenerationValue = savedRegen / SlowedRegenRate;
        CmdChangeRegenerationValue(_energy.RegenerationValue);
        float restored = 0f;
        while (restored < 1f)
        {
            restored += _energy.RegenerationValue * Time.deltaTime;
            yield return null;
        }

        _energy.RegenerationValue = savedRegen;
    }

    [Command]
    private void CmdChangeRegenerationValue(float value)
    {
        _energy.RegenerationValue = value;
        _energy.TryUse(0);
    }

    [Command(requiresAuthority = false)]
    private void CmdApplyScorched(GameObject target, GameObject source)
    {
        var character = target.GetComponent<Character>();
        if (character == null || character.IsDead) return;

        if (Random.Range(0f, 100f) <= ScorchedChance)
        {
            character.CharacterState.AddState(States.ScorchedSoul, 5f, 0f, source, nameof(RingOfFireAura));
        }
    }

    protected override void OnTargetEnter(Character target) { }
    protected override void OnTargetExit(Character target) { }
}
