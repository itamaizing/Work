using System.Collections.Generic;
using UnityEngine;

public class PoisonCloudStateStacking : StateStackingRefreshing
{
    private const int MaxStacks = 5;
    private const float BaseDamagePercent = 0.005f;
    private const float AuraRadius = 5f;
    private const float TickRate = 1f;
    private const float TimeToApplyPoisonBone = 3f;

    private static readonly List<StatusEffect> _effects = new();
    private readonly HashSet<Character> _visited = new();
    private readonly HashSet<Character> _damagedTargets = new();
    private readonly Dictionary<Character, float> _poisonBoneTimers = new();
    private readonly List<Character> _expiredTargets = new();

    private PoisonBall _poisonBall;
    private Character _caster;
    private float _tickTimer;

    public override States State => States.PoisonCloud;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public override void Apply(CharacterState character, float durationToExit, float damageToExit,
        Character sourceCaster, string skillName)
    {
        SetMaxStacks(MaxStacks);

        _caster = character.Character;
        _poisonBall = abilities != null ? abilities.GetSkill<PoisonBall>() : null;

        _tickTimer = TickRate;
        _poisonBoneTimers.Clear();
    }

    public override void UpdateState()
    {
        if (characterState == null || !characterState.isServer) return;

        _tickTimer -= Time.deltaTime;
        if (_tickTimer > 0f) return;
        _tickTimer = TickRate;

        DealAuraDamage();
    }

    private void DealAuraDamage()
    {
        if (_caster == null || _caster.IsDead)
        {
            _poisonBoneTimers.Clear();
            return;
        }

        int team = _caster.NetworkSettings.TeamIndex;
        bool canApplyBone = _poisonBall != null && _poisonBall.IsPoisonCloudAddPoisonBone;
        _visited.Clear();
        _damagedTargets.Clear();

        foreach (var col in Physics.OverlapSphere(_caster.transform.position, AuraRadius))
        {
            if (!col.TryGetComponent<Character>(out var target)) continue;
            if (!_visited.Add(target)) continue;
            if (target == _caster || target.IsDead) continue;
            if (target.NetworkSettings.TeamIndex == team) continue;

            var damage = new Damage
            {
                Value = target.Health.MaxValue * BaseDamagePercent * CurrentStacksCount,
                Type = DamageType.Physical,
            };
            if (!target.Health.TryTakeDamage(ref damage, _poisonBall) || damage.Value <= 0f)
                continue;
            if (!canApplyBone || target.IsDead) continue;

            _damagedTargets.Add(target);
            _poisonBoneTimers.TryGetValue(target, out float damageTime);
            damageTime += TickRate;
            if (damageTime > TimeToApplyPoisonBone)
            {
                target.CharacterState.AddState(States.PoisonBone, 6f, 0f, _caster.gameObject, _poisonBall.Name);
                damageTime = 0f;
            }
            _poisonBoneTimers[target] = damageTime;
        }

        _expiredTargets.Clear();
        foreach (var target in _poisonBoneTimers.Keys)
        {
            if (!_damagedTargets.Contains(target))
                _expiredTargets.Add(target);
        }
        foreach (var target in _expiredTargets)
            _poisonBoneTimers.Remove(target);
    }

    protected override void OnExit()
    {
        _poisonBoneTimers.Clear();
        _visited.Clear();
        _damagedTargets.Clear();
        _expiredTargets.Clear();
    }

    public override void ReduceStack() => ExitState();
}
