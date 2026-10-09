using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Unity.Collections;
using UnityEngine;

public class FireAures : MonoBehaviour
{
    private void Start()
    {
        //var chatacter = GetComponent<Character>();
        //chatacter.CharacterState.CmdAddState(States.Burn, 0, 0, chatacter.gameObject, name);
    }
}

public class Burn : StateBasic, ITickableState
{
    private const float DamagePerTick = 1f;
    private const float DamageRadius = 1f;
    private const float BurningDuration = 7f;

    private readonly List<StatusEffect> _effects = new() { StatusEffect.Others };
    private readonly HashSet<Character> _tickTargets = new();
    private Health _subscribedHealth;

    public override States State => States.Burn;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public float TickInterval => 1f;
    
    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character sourceCaster, string skillName) => Attach();
    public override void Reapply(CharacterState character, float durationToExit, float damageToExit, Character sourceCaster, string skillName) => Attach();

    public void Tick()
    {
        if (!characterState.isServer) return;

        Character owner = characterState.Character;
        _tickTargets.Clear();

        foreach (var col in Physics.OverlapSphere(owner.transform.position, DamageRadius))
        {
            if (!col.TryGetComponent<Character>(out var other)) continue;
            if (other.IsDead || !IsEnemy(owner, other) || !_tickTargets.Add(other)) continue;

            Damage damage = new() { Value = DamagePerTick, Type = DamageType.Magical, School = Schools.Fire };
            other.TryTakeDamage(ref damage, null);
        }
    }

    protected override void OnExit()
    {
        Detach();
    }

    private void Attach()
    {
        Health health = characterState.Character.Health;
        if (_subscribedHealth == health) return;

        Detach();
        _subscribedHealth = health;
        health.DamageTaken += OnDamageTakenServer;
    }

    private void Detach()
    {
        if (_subscribedHealth == null) return;
        _subscribedHealth.DamageTaken -= OnDamageTakenServer;
        _subscribedHealth = null;
    }

    private void OnDamageTakenServer(Damage damage, Skill skill)
    {
        if (damage.Value <= 0f) return;
        if (damage.Type != DamageType.Physical || damage.PhysicAttackType != AttackRangeType.MeleeAttack) return;

        Character attacker = skill?.Hero;
        if (attacker == null || attacker == characterState.Character) return;

        attacker.CharacterState.AddState(States.Burning, BurningDuration, 0f,
            characterState.Character.gameObject, nameof(Burning));
    }

    private static bool IsEnemy(Character owner, Character other) =>
        other != owner && other.NetworkSettings.TeamIndex != owner.NetworkSettings.TeamIndex;
}

public class Burning : StateStackingRefreshing, ITickableState
{
    private const int MaxStacks = 5;
    private const float DamagePerStack = 1f;

    private readonly List<StatusEffect> _effects = new();

    public override States State => States.Burning;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => _effects;

    public float TickInterval => 1f;

    public Burning() => SetMaxStacks(MaxStacks);
    
    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character sourceCaster, string skillName)
        => DealDamage(1);
    
    public override void Reapply(CharacterState character, float durationToExit, float damageToExit, Character sourceCaster, string skillName)
    {
        bool startingFresh = CurrentStacksCount == 0;
        base.Reapply(character, durationToExit, damageToExit, sourceCaster, skillName);
        if (startingFresh) DealDamage(1);
    }

    public void Tick() => DealDamage(CurrentStacksCount);

    protected override void OnExit()
    {
        CurrentStacksCount = 0;
    }

    private void DealDamage(int stacks)
    {
        if (characterState == null || !characterState.isServer || stacks <= 0) return;

        Damage damage = new() { Value = DamagePerStack * stacks, Type = DamageType.Magical, School = Schools.Fire };
        characterState.Character.TryTakeDamage(ref damage, null);
    }
}

