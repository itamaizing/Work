using Mirror;
using UnityEngine;

public class TreeCharacterZone : NetworkBehaviour
{
    [SerializeField] private GrowTreeAura grow;

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
}
