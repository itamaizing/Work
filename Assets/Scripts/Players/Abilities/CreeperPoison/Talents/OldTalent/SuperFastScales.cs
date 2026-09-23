using UnityEngine;

public class SuperFastScales : Talent
{
    private float _chanceOfDispelMagStates = 0.9f;
    private float _increaseResistanceToMagicDamage = 90f;
    private float _baseEvasionMagical;

    private readonly AttributeModifier _modifier = new(0, ModifierType.Flat);

    public override void Enter()
    {
        SetActive(true);
        _baseEvasionMagical = character.AttributeSystem[CharacterAttributeName.EvasionMagical].GetValue();
    }

    public override void Exit()
    {
        SetActive(false);
    }

    public void IncreasingResistance(Character target)
    {
        if (Random.Range(0.0f, 1.0f) <= _chanceOfDispelMagStates)
        {
            Debug.Log("SuperFastScales / DispelMageStates");
            if (target != null)
                character.CharacterState.DispelStates(StateType.Magic, target.NetworkSettings.TeamIndex, character.NetworkSettings.TeamIndex);
        }

        var attribute = character.AttributeSystem[CharacterAttributeName.EvasionMagical];
        _baseEvasionMagical = attribute.GetValue();
        Debug.Log("BaseEvasionMagical = " + _baseEvasionMagical);

        if (attribute.GetValue() < 100f)
        {
            attribute.RemoveBySource(this);
            _modifier.Source = this;
            _modifier.Value = _increaseResistanceToMagicDamage - attribute.GetValue();
            attribute.AddModifier(_modifier);
            Debug.Log($"Increased EvasionMagical == {attribute.GetValue()}");
        }
    }

    public void ResetResistance()
    {
        Debug.Log("Reset baseEvasionMagical = " + _baseEvasionMagical);
        character.AttributeSystem[CharacterAttributeName.EvasionMagical].RemoveBySource(this);
        Debug.Log($"Reset EvasionMagical == {character.AttributeSystem[CharacterAttributeName.EvasionMagical].GetValue()}");
    }
}