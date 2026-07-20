using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class SneakySpitCombo : Skill
{
    [Header("Dependencies")]
    [SerializeField] private CreeperStrike _creeperStrike;
    [SerializeField] private SneakySpit _sneakySpit;

    [Header("Combo Settings")]
    [SerializeField] private int _hitsForSneakySpitActivation = 3;
    [SerializeField] private float _comboResetTime = 1.5f;

    private readonly List<Character> _comboTargetsQueue = new List<Character>();

    private Character _currentComboTarget;
    private Coroutine _comboResetCoroutine;

    public int AvailablePoints => _comboTargetsQueue.Sum(target =>
    {
        if (target == null || target.CharacterState == null)
            return 0;

        var state = target.CharacterState.GetState(States.ComboState) as ComboState;
        return state?.CurrentStacksCount ?? 0;
    });

    public Character CurrentComboTarget => _currentComboTarget;

    protected override bool IsCanCast => true;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);
        TrySubscribe();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        TryUnsubscribe();
        StopComboResetTimer();
    }

    private void TrySubscribe()
    {
        if (_creeperStrike == null)
            return;

        _creeperStrike.OnHit -= HandleCreeperStrikeHit;
        _creeperStrike.OnHit += HandleCreeperStrikeHit;
    }

    private void TryUnsubscribe()
    {
        if (_creeperStrike == null)
            return;

        _creeperStrike.OnHit -= HandleCreeperStrikeHit;
    }

    private void HandleCreeperStrikeHit()
    {
        Character target = _creeperStrike.LastHitTarget;

        if (target == null)
        {
            Debug.LogWarning("[SneakySpitCombo] CreeperStrike hit ignored: LastHitTarget is NULL");
            return;
        }

        ApplyComboEffect(target.transform);
    }

    public void ApplyComboEffect(Transform enemy)
    {
        if (enemy == null)
            return;

        Character targetCharacter = enemy.GetComponent<Character>();

        if (targetCharacter == null)
            return;

        if (targetCharacter.CharacterState == null)
            return;

        if (_currentComboTarget != null && _currentComboTarget != targetCharacter)
        {
            Debug.Log(
                $"[SneakySpitCombo] Target changed. " +
                $"Old = {_currentComboTarget.name}, New = {targetCharacter.name}. Combo reset."
            );

            ClearCombo();
        }

        _currentComboTarget = targetCharacter;

        var stateManager = targetCharacter.CharacterState;
        var comboState = stateManager.GetState(States.ComboState) as ComboState;

        if (comboState == null || comboState.CurrentStacksCount <= 0)
        {
            if (!_comboTargetsQueue.Contains(targetCharacter))
                _comboTargetsQueue.Add(targetCharacter);
        }

        stateManager.AddState(
            States.ComboState,
            float.PositiveInfinity,
            0f,
            _hero.gameObject,
            nameof(SneakySpitCombo)
        );

        RestartComboResetTimer();

        Debug.Log(
            $"[SneakySpitCombo] Combo point added. " +
            $"Target = {targetCharacter.name}, " +
            $"AvailablePoints = {AvailablePoints}/{_hitsForSneakySpitActivation}, " +
            $"Frame = {Time.frameCount}, Time = {Time.time}"
        );

        TryActivateSneakySpit(targetCharacter);
    }

    private void TryActivateSneakySpit(Character target)
    {
        if (target == null)
            return;

        if (_sneakySpit == null)
        {
            Debug.LogWarning("[SneakySpitCombo] SneakySpit reference is NULL");
            return;
        }

        int availablePoints = AvailablePoints;

        Debug.Log(
            $"[SneakySpitCombo] Activation check. " +
            $"Target = {target.name}, " +
            $"Points = {availablePoints}/{_hitsForSneakySpitActivation}"
        );

        if (availablePoints < _hitsForSneakySpitActivation)
            return;

        int consumed = PayComboPoints(_hitsForSneakySpitActivation, target);

        Debug.LogError(
            $"[SneakySpitCombo] SNEAKY SPIT ACTIVATED. " +
            $"Target = {target.name}, " +
            $"Consumed = {consumed}/{_hitsForSneakySpitActivation}"
        );

        ClearCombo();

        _sneakySpit.TryStartSneakySpitBoostWindow(target);
    }

    public int PayComboPoints(int amount, Character specificTarget = null)
    {
        if (amount <= 0)
            return 0;

        int pointsConsumed;

        if (specificTarget != null)
            pointsConsumed = ConsumePointsFromTarget(specificTarget, amount);
        else
            pointsConsumed = ConsumePointsFromQueue(amount);

        return pointsConsumed;
    }

    private int ConsumePointsFromTarget(Character target, int amount)
    {
        if (target == null || target.CharacterState == null)
            return 0;

        var state = target.CharacterState.GetState(States.ComboState) as ComboState;

        if (state == null)
            return 0;

        int availablePoints = state.CurrentStacksCount;
        int pointsToConsume = Mathf.Clamp(amount, 0, availablePoints);
        int consumed = 0;

        for (int i = 0; i < pointsToConsume; i++)
        {
            bool reduced = state.Stack(-1);
            consumed++;

            if (!reduced || state.CurrentStacksCount <= 0)
            {
                target.CharacterState.RemoveState(state.State);
                _comboTargetsQueue.Remove(target);
                break;
            }
        }

        return consumed;
    }

    private int ConsumePointsFromQueue(int amount)
    {
        int pointsConsumed = 0;

        while (amount > 0 && _comboTargetsQueue.Count > 0)
        {
            Character lastTarget = _comboTargetsQueue[_comboTargetsQueue.Count - 1];

            if (lastTarget == null || lastTarget.CharacterState == null)
            {
                _comboTargetsQueue.RemoveAt(_comboTargetsQueue.Count - 1);
                continue;
            }

            var state = lastTarget.CharacterState.GetState(States.ComboState) as ComboState;

            if (state == null)
            {
                _comboTargetsQueue.RemoveAt(_comboTargetsQueue.Count - 1);
                continue;
            }

            bool reduced = state.Stack(-1);
            pointsConsumed++;
            amount--;

            if (!reduced || state.CurrentStacksCount <= 0)
            {
                lastTarget.CharacterState.RemoveState(state.State);
                _comboTargetsQueue.RemoveAt(_comboTargetsQueue.Count - 1);
            }
        }

        return pointsConsumed;
    }

    private void RestartComboResetTimer()
    {
        StopComboResetTimer();
        _comboResetCoroutine = StartCoroutine(ComboResetTimer());
    }

    private void StopComboResetTimer()
    {
        if (_comboResetCoroutine != null)
        {
            StopCoroutine(_comboResetCoroutine);
            _comboResetCoroutine = null;
        }
    }

    private IEnumerator ComboResetTimer()
    {
        yield return new WaitForSeconds(_comboResetTime);

        Debug.Log(
            $"[SneakySpitCombo] Combo reset by timeout. " +
            $"Target = {(_currentComboTarget != null ? _currentComboTarget.name : "NULL")}, " +
            $"AvailablePoints = {AvailablePoints}/{_hitsForSneakySpitActivation}"
        );

        ClearCombo();
    }

    private void ClearCombo()
    {
        StopComboResetTimer();

        foreach (Character target in _comboTargetsQueue.ToList())
        {
            if (target == null || target.CharacterState == null)
                continue;

            var state = target.CharacterState.GetState(States.ComboState) as ComboState;

            if (state != null)
                target.CharacterState.RemoveState(state.State);
        }

        _comboTargetsQueue.Clear();
        _currentComboTarget = null;

        Debug.Log("[SneakySpitCombo] Combo cleared");
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved) => null;
    protected override IEnumerator CastJob() => null;
    protected override void ClearData() { }
    public override void LoadTargetData(TargetInfo targetInfo) { }
}