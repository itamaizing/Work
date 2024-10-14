using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuperFastScales : Talent
{
    private float _increaseResistanceToMagicDamage = 90f;
    private float _baseDefMagDamage;

    private void Start()
    {
        Enter();
    }

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
        CmdIncreasingResistance();
    }

    public void ResetResistance()
    {
        CmdResetResistance();
    }

    [Command]
    private void CmdIncreasingResistance()
    {
        _baseDefMagDamage = character.Health.DefMagDamage;
        Debug.Log("BaseMagDamage = " + _baseDefMagDamage);

        if (character.Health.EvadeMagDamage < 100f)
        {
            character.Health.EvadeMagDamage += _increaseResistanceToMagicDamage;
            Debug.Log($"Increased EvadeMagDamage == {character.Health.EvadeMagDamage}");
        }
    }

    [Command]
    private void CmdResetResistance()
    {
        Debug.Log("Reset baseMagDamage = " + _baseDefMagDamage);
        character.Health.EvadeMagDamage = _baseDefMagDamage;
        Debug.Log($"Reset EvadeMagDamage == {character.Health.EvadeMagDamage}");
    }

}
