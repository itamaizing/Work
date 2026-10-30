using System.Collections.Generic;
using UnityEngine;

public class HealingPoisonCloudState : StateStackingRefreshing
{
    private const int MaxStacks = 5;
    private const float Radius = 2.5f;
    private const float HealPercentPerStack = 0.005f;
    private const float TickRate = 1f;

    private static readonly List<StatusEffect> _effects = new() { StatusEffect.Healing };
    private readonly HashSet<Character> _visited = new();

    private ExplosionPoisonCloud _explosion;
    private float _tickTimer;
    private int _lastNotifiedStacks = -1;

    public override States State => States.HealingPoisonCloud;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public override void Apply(CharacterState character, float durationToExit, float damageToExit,
        Character sourceCaster, string skillName)
    {
        SetMaxStacks(MaxStacks);
        _explosion = abilities != null ? abilities.GetSkill<ExplosionPoisonCloud>() : null;
        _tickTimer = TickRate;
        _lastNotifiedStacks = -1;
    }

    public override void UpdateState()
    {
        if (CurrentStacksCount != _lastNotifiedStacks)
        {
            _lastNotifiedStacks = CurrentStacksCount;
            _explosion?.CurrentStacksHealingPoisonCloud(CurrentStacksCount, Radius);
        }

        if (characterState == null || !characterState.isServer) return;

        _tickTimer -= Time.deltaTime;
        if (_tickTimer > 0f) return;
        _tickTimer = TickRate;

        HealInRadius();
    }

    private void HealInRadius()
    {
        var owner = characterState.Character;
        if (owner == null || owner.IsDead) return;

        int team = owner.NetworkSettings.TeamIndex;
        _visited.Clear();

        foreach (var col in Physics.OverlapSphere(owner.transform.position, Radius))
        {
            if (!col.TryGetComponent<Character>(out var target)) continue;
            if (!_visited.Add(target)) continue;
            if (target.IsDead || target.NetworkSettings.TeamIndex != team) continue;

            var heal = new Heal
            {
                Value = target.Health.MaxValue * HealPercentPerStack * CurrentStacksCount,
                DamageableSkill = null
            };
            target.Health.Heal(ref heal, "HealingPoisonCloud");
        }
    }

    public override void ReduceStack() => ExitState();
}