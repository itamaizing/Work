using Mirror;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PlagueCloudDamagePrefab : NetworkBehaviour
{
    [SerializeField] private LayerMask _alliesMask;
    private const float Duration = 12f;

    [Server]
    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<Character>(out var character)) return;
        if (character == null || character.CharacterState == null) return;
        if (((1 << other.gameObject.layer) & _alliesMask) != 0) return;

        character.CharacterState.CmdAddState(States.Plague, Duration, 0, null, "PlagueCloud");
    }
}