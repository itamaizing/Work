using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class FireComboHandler : NetworkBehaviour
{
    private const float ChargeCD = 6f;
    private const int MaxCharges = 3;

    [SerializeField] private HeroComponent _hero;
    private PassiveCombo_Scorpion _passiveCombo;

    private bool _isEnabled;
    public bool IsEnabled => _isEnabled;
    private bool _isMultiTargetMode;

    private readonly Dictionary<Skill, int> _comboCharges = new();
    private readonly Dictionary<Skill, List<float>> _chargeEndTimes = new();

    private readonly Dictionary<Skill, Action> _castSuccessHandlers = new();
    private Action _ignitionCastHandler;
    private Action _fireBreathCastStartedHandler;
    private Action _ringEnabledHandler;
    private Action<GameObject> _ringDamageHandler;

    private void Awake()
    {
        _passiveCombo = GetComponent<PassiveCombo_Scorpion>();
    }

    public void SetEnabled(bool value)
    {
        if (_isEnabled == value) return;
        _isEnabled = value;
        if (value) Subscribe();
        else Unsubscribe();
    }

    private void Subscribe()
    {
        foreach (var skill in _hero.Abilities.Abilities.Where(s => s.Info.School == Schools.Fire))
        {
            skill.Charges?.EnableChargers(true, MaxCharges, ChargeCD);

            _comboCharges[skill] = MaxCharges;
            _chargeEndTimes[skill] = new List<float>();

            if (skill is IgnitionSkill ign)
            {
                _ignitionCastHandler = () => OnFireSkillActivated(ign, ign.Targeting.GetTarget()?.Character);
                ign.CastStarted += _ignitionCastHandler;
            }
            else if (skill is FireBreath_Scorpion breath)
            {
                _fireBreathCastStartedHandler = () => OnFireSkillActivated(breath, null);
                breath.OnFireBreathStarted += _fireBreathCastStartedHandler;
            }
            else if (skill is RingOfFireSkill ring)
            {
                _ringEnabledHandler = () => OnFireSkillActivated(ring, null);
                ring.OnRingEnabled += _ringEnabledHandler;
            }
            /*else
            {
                var capturedSkill = skill;
                Action handler = () => OnFireSkillActivated(capturedSkill, capturedSkill.Targeting.GetTarget()?.Character);
                
                _castSuccessHandlers[skill] = handler;
                skill.CastSuccess += handler;
            }*/
        }
    }
    
    private Character FindNearestEnemy()
    {
        if (_hero == null) return null;

        Collider[] colliders = Physics.OverlapSphere(_hero.transform.position, 6f);

        Character nearest = null;
        float minDistance = float.MaxValue;

        foreach (var col in colliders)
        {
            if (col.TryGetComponent<Character>(out var character) &&
                !character.IsDead && col.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                float distance = Vector3.Distance(_hero.transform.position, character.transform.position);
            
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = character;
                }
            }
        }

        return nearest;
    }

    private void Unsubscribe()
    {
        if (_ignitionCastHandler != null)
        {
            var ign = _hero.Abilities.GetSkill<IgnitionSkill>();
            if (ign != null) ign.CastStarted -= _ignitionCastHandler;
            ign.Charges?.EnableChargers(false, 0, ChargeCD);
        }

        if (_fireBreathCastStartedHandler != null)
        {
            var breath = _hero.Abilities.GetSkill<FireBreath_Scorpion>();
            if (breath != null) breath.OnFireBreathStarted -= _fireBreathCastStartedHandler;
            breath.Charges?.EnableChargers(false, 0, ChargeCD);
        }

        if (_ringEnabledHandler != null)
        {
            var ring = _hero.Abilities.GetSkill<RingOfFireSkill>();
            if (ring != null) ring.OnRingEnabled -= _ringEnabledHandler;
            ring.Charges?.EnableChargers(false, 0, ChargeCD);
        }

        /*foreach (var kvp in _castSuccessHandlers)
        {
            kvp.Key.CastSuccess -= kvp.Value;
        }*/

        _castSuccessHandlers.Clear();
        _comboCharges.Clear();
        _chargeEndTimes.Clear();

        /*foreach (var skill in _hero.Abilities.Abilities.Where(s => s.Info.School == Schools.Fire))
        {
            skill.Charges?.EnableChargers(false, MaxCharges, ChargeCD);
        }*/
    }

    private void OnFireSkillActivated(Skill skill, Character target)
    {
        if (!_isEnabled || !isOwned || skill == null) return;

        Character finalTarget = target ?? _passiveCombo?.CurrentTarget;

        if (finalTarget == null)
        {
            finalTarget = FindNearestEnemy();
        }

        if (finalTarget == null) return;
        
        _passiveCombo?.RegisterFireComboHit(skill,finalTarget);
    }
    private void Update()
    {
        if (!_isEnabled || !isOwned) return;

        float now = Time.time;
        foreach (var list in _chargeEndTimes.Values)
        {
            list.RemoveAll(t => t <= now);
        }
    }
}