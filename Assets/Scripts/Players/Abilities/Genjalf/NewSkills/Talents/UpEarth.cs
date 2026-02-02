using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpEarth : Talent
{
    [SerializeField] private float _increasePercentages = 1.1f;

    private WaitForSeconds _increaseManaRegenerationDeley;
    private Resource _mana;
    private AttributeModifiers attributeModifiers;

    public override void Enter()
    {
        attributeModifiers = new AttributeModifiers(character.Health.MaxValue * _increasePercentages, ModifierType.Flat);
        character.Health.AddModifier(attributeModifiers);
        //character.Health.ChangedMaxValue(character.Health.MaxValue * _increasePercentages);
    }

    public override void Exit()
    {
        character.Health.RemoveModifier(attributeModifiers);
        //character.Health.ChangedMaxValue(character.Health.MaxValue / _increasePercentages);
    }
}
