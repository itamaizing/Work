using System.Collections.Generic;

public class ParalyzingPoisonState : StateBasic
{
    public bool turnOff = false;

    private readonly List<StatusEffect> _effects = new()
    {
        StatusEffect.Move,
        StatusEffect.Ability,
        StatusEffect.Poison
    };

    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override States State => States.ParalyzingPoison;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    public override bool CanExceedMaxDuration => true;

    public override void Apply(
        CharacterState character,
        float durationToExit,
        float damageToExit,
        Character personWhoMadeBuff,
        string skillName)
    {
        turnOff = false;

        abilities?.SetAbilitiesDisactive(true);

        characterState.Character.Move.IsMoveBlocked = true;
        characterState.Character.Move.StopMoveAndAnimationMove();
    }

    public override void UpdateState()
    {
        if (turnOff)
            ExitState();
    }

    protected override void OnExit()
    {
        bool moveBlocked = false;
        bool abilitiesBlocked = false;

        foreach (var otherState in characterState.CurrentStates)
        {
            if (otherState == this)
                continue;

            if (otherState.Effects.Contains(StatusEffect.Move))
                moveBlocked = true;

            if (otherState.Effects.Contains(StatusEffect.Ability))
                abilitiesBlocked = true;
        }

        characterState.Character.Move.IsMoveBlocked = moveBlocked;
        abilities?.SetAbilitiesDisactive(abilitiesBlocked);

        turnOff = false;
    }
}