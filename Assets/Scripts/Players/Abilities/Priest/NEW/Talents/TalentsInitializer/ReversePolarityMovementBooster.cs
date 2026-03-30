using Mirror;
using UnityEngine;

public class ReversePolarityMovementBooster : SkillTalentHandler
{
    private bool _enabled;
    
    private AttributeModifier _speedModifier = new(0, ModifierType.Percent);

    private const float _speedBonus = 0.3f;

    public ReversePolarityMovementBooster(NetworkBehaviour owner) : base(owner) { }

    public void Enable(bool value)
    {
        _enabled = value;
    }
    
    public void OnReversePolarityActivated(bool isActivated)
    {
        if (!_enabled || !Owner.isOwned) 
            return;

        var character = Owner.GetComponent<Character>();
        if (character == null) return;

        if (isActivated)
        {
            RemoveAllSlowEffects(character);
            ApplyMovementSpeedBuff(character);
        }

        else
            RemoveMovementSpeedBuff(character);

    }
    private void RemoveAllSlowEffects(Character character)
    {
        if (character.CharacterState == null) return;

        foreach (var state in character.CharacterState.CurrentStates)
        {
            if (state.Effects.Contains(StatusEffect.MoveSpeed))
            {
                character.CharacterState.RemoveState(state);
            }
        }
    }

    private void ApplyMovementSpeedBuff(Character character)
    {
        _speedModifier.Value = _speedBonus;
        character.Move.AddModifier(_speedModifier);
    }

    private void RemoveMovementSpeedBuff(Character character)
    {
        character.Move.RemoveModifier(_speedModifier);
    }
}
