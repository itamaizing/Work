using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreeperInvisible : Ability
{
    [SerializeField] private Character _player;

    private Coroutine _useCoroutine;
    private Coroutine _startCoroutine;

    protected override void Cast()
    {
        if (_startCoroutine == null)
            _startCoroutine = StartCoroutine(StartAbility());
    }

    protected override void Cancel()
    {
        Debug.Log("Cancel Invisible");
        if (_useCoroutine != null)
        {
            StopCoroutine(EnteringInvisibleState());
            _useCoroutine = null;
        }
        if (_startCoroutine != null)
        {
            StopCoroutine(StartAbility());
            _startCoroutine = null;
        }
    }

    private IEnumerator StartAbility()
    {
        _useCoroutine = StartCoroutine(EnteringInvisibleState());
        yield return null;
    }

    private IEnumerator EnteringInvisibleState()
    {
        ApplyInvis();
        yield return null;
    }

    [Command]
    private void ApplyInvis()
    {
        _player.CharacterState.CmdAddState(States.CreeperInvisible, 0, 0);
    }
}
