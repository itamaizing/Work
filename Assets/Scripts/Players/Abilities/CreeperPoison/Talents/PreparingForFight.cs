using Org.BouncyCastle.Security;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreparingForFight : Talent
{
    private Character _player;
    private Resource _playerMana;
    private float _maxManaPlayer;
    private float _manaRecoveryMultiplier = 0.01f;

    private void Start()
    {
        //Enter();
    }

    public override void Enter()
    {
        SetActive(true);
        _player = character;
        _playerMana = _player.TryGetResource(ResourceType.Mana);
    }

    public override void Exit()
    {
        SetActive(false);
    }

    public void IncreaseManaRegeneration()
    {
        _maxManaPlayer = _playerMana.MaxValue;
        float updatedManaRecoveryValue = _maxManaPlayer * _manaRecoveryMultiplier;
        Debug.Log("PlayerManaValue before AddMana = " + _playerMana.CurrentValue);

        _playerMana.Add(updatedManaRecoveryValue);
        Debug.Log("PlayerManaValue after AddMana = " + _playerMana.CurrentValue);
    }

}
