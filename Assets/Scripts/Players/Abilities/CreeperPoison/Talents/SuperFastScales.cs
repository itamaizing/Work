using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuperFastScales : Talent
{
    private float _increaseResistanceToMagicDamage = 90f;
    private float _baseDefMagDamage;

    public override void Enter()
    {
        SetActive(true);
        _baseDefMagDamage = character.Health.DefMagDamage;
    }

    public override void Exit()
    {
        SetActive(false);
    }

    public void IncreasingResistance()
    {
        character.Health.SetDefMagicDamage(_increaseResistanceToMagicDamage);
        Debug.Log($"Increased DefMagDamage == {character.Health.DefMagDamage}");
    }

    public void ResetResistance()
    {
        character.Health.SetDefMagicDamage(-_increaseResistanceToMagicDamage);
        Debug.Log($"Reset DefMagDamage == {character.Health.DefMagDamage}");
    }
}
