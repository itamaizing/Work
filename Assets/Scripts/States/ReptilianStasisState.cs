using System.Collections.Generic;

public class ReptilianStasisStateStacking : StateStacking
{
    private static readonly List<StatusEffect> _effects = new() { StatusEffect.Stunning };
    private Character _owner;

    public override States State => States.ReptilianStasis;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => _effects;

    public override void Apply(CharacterState character, float durationToExit, float damageToExit,
        Character sourceCaster, string skillName)
    {
        _owner = character.Character;
        if (_owner == null) return;

        SetMaxStacks(1);

        if (_owner.Move != null)
        {
            _owner.Move.SetCanMove(false);
            _owner.Move.IsMoveBlocked = true;
        }
        _owner.Abilities?.SetAbilitiesDisactive(true);
    }

    protected override void OnExit()
    {
        if (_owner == null) return;

        if (_owner.Move != null)
        {
            _owner.Move.SetCanMove(true);
            _owner.Move.IsMoveBlocked = false;
        }
        _owner.Abilities?.SetAbilitiesDisactive(false);
        
        if (_owner.isOwned && _owner.Abilities != null)
        {
            foreach (var skill in _owner.Abilities.Skills)
            {
                if (skill == null || skill.Cooldown == null || skill is ReptilianStasis) continue;
                if (skill.Cooldown.IsActive) skill.Cooldown.ForceEnd();
            }
        }
    }

    public override void ReduceStack() => ExitState();

    public override bool Stack(float time)
    {
        RemainingDuration = time;
        return true;
    }
}