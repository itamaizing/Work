using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuperFastScales : Talent
{
    private float _chanceOfDispelMagStates = 0.9f;
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

    public void IncreasingResistance(Character target)
    {
        if (Random.Range(0.0f, 1.0f) <= _chanceOfDispelMagStates)
        {
            Debug.Log("SuperFastScales / DispelMageStates");
            if (target != null)
                character.CharacterState.DispelStates(StateType.Magic, target.NetworkSettings.TeamIndex, character.NetworkSettings.TeamIndex);
        }

        _baseDefMagDamage = character.Health.DefMagDamage;
        Debug.Log("BaseMagDamage = " + _baseDefMagDamage);

        if (character.Health.EvadeMagDamage < 100f)
        {
            character.Health.EvadeMagDamage = _increaseResistanceToMagicDamage;
            Debug.Log($"Increased ResistMagDamage == {character.Health.EvadeMagDamage}");
        }
    }

    public void ResetResistance()
    {
        Debug.Log("Reset baseMagDamage = " + _baseDefMagDamage);
        character.Health.EvadeMagDamage = _baseDefMagDamage;
        Debug.Log($"Reset ResistMagDamage == {character.Health.EvadeMagDamage}");
    }
}
