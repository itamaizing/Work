using System.Collections;
using Mirror;
using UnityEngine;

public abstract class SkillTalentHandler
{
    protected readonly NetworkBehaviour Owner;

    protected SkillTalentHandler(NetworkBehaviour owner)
    {
        Owner = owner;
    }

    protected Coroutine StartCoroutine(IEnumerator routine) 
        => Owner.StartCoroutine(routine);
}