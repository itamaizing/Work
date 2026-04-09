using Mirror;
using UnityEngine;

public class TreeCharacterZone : NetworkBehaviour
{
    [SerializeField] private GrowTreeAura grow;

    private Character _anchoredCharacter;

    [Server]
    public void AnchorCharacter(Character character)
    {
        if (character == null) return;

        _anchoredCharacter = character;
        RpcAnchorCharacter(character.netId);
    }

    [Server]
    public void ReleaseCharacter()
    {
        if (_anchoredCharacter == null) return;

        RpcReleaseCharacter(_anchoredCharacter.netId);
        _anchoredCharacter = null;
    }

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        var character = other.GetComponent<Character>();
        if (character == null) return;
        grow.RpcApplyTreeBuff(character);
    }

    [ServerCallback]
    private void OnTriggerExit(Collider other)
    {
        var character = other.GetComponent<Character>();
        if (character == null) return;
        grow.RpcRemoveTreeBuff(character);
    }

    [ClientRpc]
    private void RpcAnchorCharacter(uint netId)
    {
        if (!NetworkClient.spawned.TryGetValue(netId, out var obj)) return;

        var character = obj.GetComponent<Character>();
        if (character == null) return;

        character.Move.StopMoveAndAnimationMove();
        character.Move.IsMoveBlocked = true;

        character.transform.position = transform.position;
    }

    [ClientRpc]
    private void RpcReleaseCharacter(uint netId)
    {
        if (!NetworkClient.spawned.TryGetValue(netId, out var obj)) return;

        var character = obj.GetComponent<Character>();
        if (character == null) return;

        character.Move.IsMoveBlocked = false;
    }
}
