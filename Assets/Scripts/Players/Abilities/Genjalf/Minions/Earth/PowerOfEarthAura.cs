using System.Collections.Generic;
using UnityEngine;

public class PowerOfEarthAura : AuraStateHandler
{
    [SerializeField] private float _buffDuration = -1f;

    protected override void OnTargetEnter(Character target)
    {
        CmdApplyStateToTarget(target.gameObject, States.PowerOfEarth, _buffDuration, Schools.Earth, _owner.gameObject, nameof(PowerOfEarthAura));
    }

    protected override void OnTargetExit(Character target)
    {
        CmdRemoveStateFromTarget(target.gameObject, States.PowerOfEarth);
    }

    protected override void OnAuraDisabled()
    {
        RemoveEffectsFromAllTargets();
    }
}

public class PowerOfEarth : AbstractCharacterState
{
    private Character _character;
    
    private List<StatusEffect> _effects = new List<StatusEffect>();
    private int _stanChance = 20;
    private float _stanDuration = 1.5f;
    private float _addDamage = .5f;
    public override States State => States.PowerOfEarth;
    public override StateType Type { get; }
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    protected override void OnEnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff,
        string skillName)
    {
        _character = characterState.Character;
        _character.Health.DamageTaken += OnDamageGeted;
    }

    public override void OnUpdateState() { }

    protected override void OnExitState()
    {
        _character.Health.DamageTaken -= OnDamageGeted;
    }

    private void OnDamageGeted(Damage damage, Skill skill)
    {
        Debug.LogError("WasAttacked");

        var randomInt = Random.Range(0, 100);

        if (damage.PhysicAttackType != AttackRangeType.MeleeAttack || randomInt > _stanChance)
            return;
        if(_character.isClient)
            skill.Hero.CharacterState.CmdAddState(States.Stun, _stanDuration, 0, skill.Hero.gameObject, "name");
    }
}
