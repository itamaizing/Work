using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Mucus : NetworkBehaviour
{
    [Header("Tick Settings")]
    [SerializeField] private float _tick = 1f;

    private readonly List<Character> charactersInZone = new();
    private readonly HashSet<uint> clientIds = new();
    private Coroutine _routine;

    [Server]
    private void RemoveAuthority()
    {
        var id = netIdentity;
        if (id.connectionToClient != null) id.RemoveClientAuthority();
    }

    private void OnDestroy()
    {
        RemoveAuthority();
        if (_routine != null) StopCoroutine(_routine);


        foreach (var character in charactersInZone) ForceExit(character);
        foreach (var id in clientIds.ToArray()) RemoveCharacter(id);

        charactersInZone.Clear();
        clientIds.Clear();
    }


    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PsionicEnergySkill>(out var psiSkill) &&
            psiSkill.Hero != null &&
            psiSkill.Hero.TryGetComponent<Character>(out var character) &&
            !charactersInZone.Contains(character))
        {
            charactersInZone.Add(character);
            RpcAddCharacter(character.netId);

            if (_routine == null) _routine = StartCoroutine(ApplyHealingSlimePeriodically());
        }
    }

    [ServerCallback]
    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<PsionicEnergySkill>(out var psiSkill) &&
            psiSkill.Hero != null &&
            psiSkill.Hero.TryGetComponent<Character>(out var character))
        {
            charactersInZone.Remove(character);
            ForceExit(character);
            RpcRemoveCharacter(character.netId);

            if (charactersInZone.Count == 0 && _routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
        }
    }

    private void ForceExit(Character character)
    {
        if (character == null) return;
        if (character.TryGetComponent<CharacterState>(out var state) &&
            state.GetState(States.HealingSlime) is HealingSlime healingSlime)
        {
            healingSlime.SwitchToFinite();
        }
    }

    private IEnumerator ApplyHealingSlimePeriodically()
    {
        var wait = new WaitForSeconds(_tick);

        while (charactersInZone.Count > 0)
        {
            foreach (Character character in charactersInZone)
            {
                if (character == null || !character.TryGetComponent(out CharacterState state)) continue;
                state.AddState(States.HealingSlime, 9999, 0f, gameObject, name);
            }

            yield return wait;
        }

        _routine = null;
    }

    [ClientRpc]
    private void RpcAddCharacter(uint netId)
    {
        if (!NetworkClient.spawned.TryGetValue(netId, out var id)) return;
        if (!clientIds.Add(netId)) return;
    }

    [ClientRpc] private void RpcRemoveCharacter(uint netId) => RemoveCharacter(netId);

    private void RemoveCharacter(uint netId)
    {
        if (!clientIds.Remove(netId)) return;

        if (NetworkClient.spawned.TryGetValue(netId, out var id) &&
            id.TryGetComponent(out CharacterState state))
        {
            (state.GetState(States.HealingSlime) as HealingSlime)?.SwitchToFinite();
        }
    }
}
