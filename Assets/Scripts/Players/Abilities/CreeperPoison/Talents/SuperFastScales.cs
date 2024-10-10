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
        _baseDefMagDamage = Character.Health.DefMagDamage;
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
        _baseDefMagDamage = Character.Health.DefMagDamage;
        Debug.Log("BaseMagDamage = " + _baseDefMagDamage);

        if (Character.Health.EvadeMagDamage < 100f)
        {
            Character.Health.EvadeMagDamage += _increaseResistanceToMagicDamage;
            Debug.Log($"Increased EvadeMagDamage == {Character.Health.EvadeMagDamage}");
        }
    }

    [Command]
    private void CmdResetResistance()
    {
        Debug.Log("Reset baseMagDamage = " + _baseDefMagDamage);
        Character.Health.EvadeMagDamage = _baseDefMagDamage;
        Debug.Log($"Reset EvadeMagDamage == {Character.Health.EvadeMagDamage}");
    }

}
