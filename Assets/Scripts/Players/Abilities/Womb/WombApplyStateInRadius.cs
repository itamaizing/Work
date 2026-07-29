using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WombApplyStateInRadius : Skill, IPassiveSkill
{
    [SerializeField] private float _tick = 0.1f;
    [SerializeField] private float _radiusGrowthInterval = 1f;
    [SerializeField] private float _maxRadius = 6f;
    [SerializeField] private CreatureSpawn _creatureSpawn;

    private readonly HashSet<Character> _inZoneCharacters = new();
    private readonly Dictionary<Character, Coroutine> _slimeCoroutines = new();
    private readonly Dictionary<Character, Coroutine> _parasiteCoroutines = new();
    private float _currentRadius = 0f;
    private Coroutine _mainRoutine;
    private Coroutine _radiusRoutine;

    private const float StackInterval = 1f;
    
    private bool _isWombSpreadsParasites = false;

    private void Start()
    {
        Subscription();
    }

    private void OnDisable()
    {
        Unsubscribe();

        if (_mainRoutine != null) StopCoroutine(_mainRoutine);
        if (_radiusRoutine != null) StopCoroutine(_radiusRoutine);
        ClearAllStates();
    }

    private void InvokeHandleWombSpreadsMucusChanged()
    {
        HandleWombSpreadsMucusChanged(_creatureSpawn.WombSpawn.IsWombSpreadsMucus);
        HandleWombSpreadsParasitesChanged(_creatureSpawn.WombSpawn.IsWombSpreadsParasites);
    }

    private void Subscription()
    {
        if (_creatureSpawn.WombSpawn != null)
        {
            _creatureSpawn.WombSpawn.OnWombSpreadsMucusChanged += HandleWombSpreadsMucusChanged;
            _creatureSpawn.WombSpawn.OnWombSpreadsParasitesChanged += HandleWombSpreadsParasitesChanged;

            InvokeHandleWombSpreadsMucusChanged();
        }
    }

    private void Unsubscribe()
    {
        if (_creatureSpawn.WombSpawn != null)
        {
            _creatureSpawn.WombSpawn.OnWombSpreadsMucusChanged -= HandleWombSpreadsMucusChanged;
            _creatureSpawn.WombSpawn.OnWombSpreadsParasitesChanged -= HandleWombSpreadsParasitesChanged;
        }
    }

    private IEnumerator RadiusGrowthRoutine()
    {
        _currentRadius = 0f;
        WaitForSeconds wait = new(_radiusGrowthInterval);
        while (_currentRadius < _maxRadius)
        {
            _currentRadius += 1f;
            yield return wait;
        }
    }

    private IEnumerator CheckZoneRoutine()
    {
        WaitForSeconds wait = new(_tick);
        while (true)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, _currentRadius);
            HashSet<Character> current = new();

            foreach (var hit in hits)
            {
                if (!hit.TryGetComponent(out Character target)) continue;

                var hasPsi = target.GetComponent<PsionicEnergySkill>() != null ||
                             (target.CharacterParent != null && target.CharacterParent.GetComponent<PsionicEnergySkill>() != null);
                if (!hasPsi) continue;

                current.Add(target);

                if (_inZoneCharacters.Add(target))
                {
                    HandleZoneEntry(target);

                    var slimeRoutine = StartCoroutine(ApplyHealingSlimeRoutine(target));
                    _slimeCoroutines[target] = slimeRoutine;

                    if (_isWombSpreadsParasites)
                    {
                        var parasiteRoutine = StartCoroutine(ApplyParasitesRoutine(target));
                        _parasiteCoroutines[target] = parasiteRoutine;
                    }
                }
            }

            foreach (var character in _inZoneCharacters)
            {
                if (character == null || current.Contains(character)) continue;
                RemoveHealingSlime(character);
            }

            _inZoneCharacters.RemoveWhere(character => character == null || !current.Contains(character));

            yield return wait;
        }
    }
    
    private void HandleZoneEntry(Character character)
    {
        if (!character.TryGetComponent(out CharacterState state)) return;

        if (state.GetState(States.HealingSlime) is HealingSlime existingSlime)
        {
            existingSlime.SwitchToInfinite();
            CmdSwitchSlimeToInfinite(state);
            return;
        }

        state.CmdAddState(States.HealingSlime, 9999f, 0f, gameObject, name);

        if (state.GetState(States.HealingSlime) is HealingSlime newSlime)
            newSlime.NextStackDueTime = Time.time + StackInterval;
    }

    private IEnumerator ApplyParasitesRoutine(Character character)
    {
        WaitForSeconds wait = new(3f);

        while (_inZoneCharacters.Contains(character))
        {
            if (character.TryGetComponent(out CharacterState state)) state.CmdAddState(States.Parasites, 12f, 0f, gameObject, name);

            yield return wait;
        }
    }

    private IEnumerator ApplyHealingSlimeRoutine(Character character)
    {
        if (!character.TryGetComponent(out CharacterState state)) yield break;

        float initialWait = StackInterval;
        if (state.GetState(States.HealingSlime) is HealingSlime existingSlime && existingSlime.NextStackDueTime > Time.time)
        {
            initialWait = existingSlime.NextStackDueTime - Time.time;
        }

        if (initialWait > 0f) yield return new WaitForSeconds(initialWait);

        WaitForSeconds wait = new(StackInterval);

        while (_inZoneCharacters.Contains(character))
        {
            if (character.TryGetComponent(out CharacterState st) && st.GetState(States.HealingSlime) is HealingSlime slime)
            {
                if (slime.CurrentStacksCount < slime.MaxStacksCount)
                    st.CmdAddState(States.HealingSlime, 9999f, 0f, gameObject, name);

                slime.NextStackDueTime = Time.time + StackInterval;
            }

            yield return wait;
        }
    }

    private void HandleWombSpreadsMucusChanged(bool active)
    {
        if (active) StartCorutines();
        else StopCorutines();
    }

    private void HandleWombSpreadsParasitesChanged(bool active) => _isWombSpreadsParasites = active;

    private void AddHealingSlime(Character character)
    {
        if (!character.TryGetComponent(out CharacterState state)) return;

        if (state.GetState(States.HealingSlime) is HealingSlime) state.CmdAddState(States.HealingSlime, 9999f, 0f, gameObject, name);
        else state.CmdAddState(States.HealingSlime, 9999f, 0f, gameObject, name);
    }

    private void RemoveHealingSlime(Character character)
    {
        if (_slimeCoroutines.TryGetValue(character, out Coroutine routine))
        {
            StopCoroutine(routine);
            _slimeCoroutines.Remove(character);
        }

        if (_parasiteCoroutines.TryGetValue(character, out Coroutine parasiteRoutine))
        {
            StopCoroutine(parasiteRoutine);
            _parasiteCoroutines.Remove(character);
        }

        if (character.TryGetComponent(out CharacterState state))
        {
            if (state.GetState(States.HealingSlime) is HealingSlime healingSlime)
            {
                healingSlime.SwitchToFinite();
                CmdSwitchSlimeToFinite(state);
            }
        }
    }

    [Command]
    private void CmdSwitchSlimeToFinite(CharacterState state)
    {
        if (state.GetState(States.HealingSlime) is HealingSlime healingSlime)
            healingSlime.SwitchToFinite();
    }
    
    [Command]
    private void CmdSwitchSlimeToInfinite(CharacterState state)
    {
        if (state.GetState(States.HealingSlime) is HealingSlime healingSlime)
            healingSlime.SwitchToInfinite();
    }

    private void ClearAllStates()
    {
        foreach (var character in _inZoneCharacters)
        {
            RemoveHealingSlime(character);
        }

        _inZoneCharacters.Clear();
        _slimeCoroutines.Clear();
        _parasiteCoroutines.Clear();
    }

    private void StartCorutines()
    {
        _currentRadius = 0f;

        if (_mainRoutine == null)
            _mainRoutine = StartCoroutine(CheckZoneRoutine());

        if (_radiusRoutine == null)
            _radiusRoutine = StartCoroutine(RadiusGrowthRoutine());
    }

    private void StopCorutines()
    {
        if (_mainRoutine != null)
        {
            StopCoroutine(_mainRoutine);
            _mainRoutine = null;
        }

        if (_radiusRoutine != null)
        {
            StopCoroutine(_radiusRoutine);
            _radiusRoutine = null;
        }

        ClearAllStates();
        _currentRadius = 0f;
    }

    #region NotUsedSkillOverrides
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    public override void LoadTargetData(TargetInfo targetInfo) { }
    protected override IEnumerator CastJob() => null;
    protected override void ClearData() { }
    protected override IEnumerator PrepareJob(Action<TargetInfo> callback) => null;
    #endregion
}
