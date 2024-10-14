using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToxiqueCloud : Talent
{
    private void Start()
    {
        Enter();
    }
    public override void Enter()
    {
        SetActive(true);
    }

    public override void Exit()
    {
        SetActive(false);
    }

    public void ApplyBonePoison(Character targetHealth)
    {
        Debug.Log("ApplyBonePoison in ToxiqueCloud");
        targetHealth.CharacterState.CmdAddState(States.PoisonBone, 6f, 0, character.gameObject, null);
    }
}
