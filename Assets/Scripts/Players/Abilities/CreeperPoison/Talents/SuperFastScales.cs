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
        _baseDefMagDamage = Character.Health.DefMagDamage;
    }

    public override void Exit()
    {
        SetActive(false);
    }

    public void IncreasingResistance()
    {
        Character.Health.SetDefMagicDamage(_increaseResistanceToMagicDamage);
        Debug.Log($"Increased DefMagDamage == {Character.Health.DefMagDamage}");
    }

    public void ResetResistance()
    {
        Character.Health.SetDefMagicDamage(-_increaseResistanceToMagicDamage);
        Debug.Log($"Reset DefMagDamage == {Character.Health.DefMagDamage}");
    }
}
