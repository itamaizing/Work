using System.Collections;
using Mirror;
using UnityEngine;

public class MagicDomeZone : AuraStateHandler
{
    [SyncVar] private float _durability;
    [SyncVar] private float _shieldDuration;
    
    private Coroutine _checkRoutine;

    [Server]
    public void PreInit(float durability, float duration)
    {
        _durability = durability;
        _shieldDuration = duration;
    }

    [Server]
    public void BeginLifetime()
    {
        StartCoroutine(DestroyAfterDelay(_shieldDuration));
    }

    public void DecreaseDurability(float value)
    {
        _durability -= value;
        if (_durability <= 0)
        {
            StartCoroutine(DestroyAfterDelay(0));
        }
    }

    [Server]
    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        NetworkServer.Destroy(gameObject);
    }

    protected override void OnTargetEnter(Character target)
    {
        if (target.CharacterState == null) return;
        if (target.CharacterState.CheckForState(States.MagicShield)) return;
        bool isAlly  = target.gameObject.layer == LayerMask.NameToLayer("Allies");
        bool isEnemy = target.gameObject.layer == LayerMask.NameToLayer("Enemy");
        if (!isAlly && !isEnemy) return;
        string tag = isEnemy
            ? $"{nameof(MagicShieldState)}_enemy_zone"
            : $"{nameof(MagicShieldState)}_zone";
        CmdApplyStateToTarget(target.gameObject, States.MagicShield, _shieldDuration, Schools.Dark, _owner.gameObject, tag,_durability);
    }

    protected override void OnTargetExit(Character target)
    {
        if (target.CharacterState == null) return;
        target.GetComponent<CharacterState>().CmdRemoveState(States.MagicShield);
        CmdRemoveStateFromTarget(target.gameObject, States.MagicShield);
    }
}