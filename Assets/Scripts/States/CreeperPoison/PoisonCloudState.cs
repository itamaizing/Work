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

    private PoisonBall _poisonBall;
    private Character _caster;
    private float _tickTimer;
    private float _poisonBoneTimer;

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
        _poisonBoneTimer = 0f;
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
        if (_caster == null || _caster.IsDead) return;

        int team = _caster.NetworkSettings.TeamIndex;
        bool applyBone = _poisonBoneTimer >= TimeToApplyPoisonBone
                         && _poisonBall != null && _poisonBall.IsPoisonCloudAddPoisonBone;
        bool dealt = false;
        _visited.Clear();

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
            target.Health.TryTakeDamage(ref damage, _poisonBall);
            dealt = true;

            if (applyBone)
                target.CharacterState.AddState(States.PoisonBone, 6f, 0f, _caster.gameObject, null);
        }

        if (dealt)
        {
            _poisonBoneTimer += TickRate;
            if (_poisonBoneTimer >= TimeToApplyPoisonBone) _poisonBoneTimer = 0f;
        }
    }

    public override void ReduceStack() => ExitState();
}