using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GrowTreeAura : NetworkBehaviour
{
    [Header("Tick")]
    [SerializeField] private float _tick = 1f;
    [SerializeField] private LayerMask characterLayer;

    private SkillManager _skillManager;
    private readonly List<Character> charactersInZone = new();
    private readonly HashSet<uint> clientIds = new();
    private Coroutine _routine;

    private const float TreeMultiplier = 1.2f;
    private readonly HashSet<Character> buffedCharacters = new();

    [Header("Talent")]
    private bool _growTreeIncreasesMaxHealth;

    public bool GrowTreeIncreasesMaxHealth { get => _growTreeIncreasesMaxHealth; set => _growTreeIncreasesMaxHealth = value; }

    [Server]
    private void RemoveAuthority()
    {
        var id = netIdentity;
        if (id.connectionToClient != null) id.RemoveClientAuthority();
    }

    public void Init(SkillManager skillManager)
    {
        _skillManager = skillManager;
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

    private void ForceExit(Character character)
    {
        if (character == null) return;
        if (character.TryGetComponent<CharacterState>(out var state) && state.GetState(States.ShadowTree) is ShadowTree shadow) shadow.SwitchToFinite();
    }

    private void ApplyBuff(Character character)
    {
        if (buffedCharacters.Contains(character)) return;

        if (!character.TryGetComponent(out SkillManager skillManager)) return;

        foreach (var skill in skillManager.Abilities)
        {
            if (skill == null) continue;

            skill.Buff.Length.IncreasePercentage(TreeMultiplier);
            skill.Buff.Radius.IncreasePercentage(TreeMultiplier);
        }

        buffedCharacters.Add(character);
    }

    private void RemoveBuff(Character character)
    {
        if (!buffedCharacters.Contains(character)) return;

        if (!character.TryGetComponent(out SkillManager skillManager)) return;

        foreach (var skill in skillManager.Abilities)
        {
            if (skill == null) continue;

            skill.Buff.Length.ReductionPercentage(TreeMultiplier);
            skill.Buff.Radius.ReductionPercentage(TreeMultiplier);
        }

        buffedCharacters.Remove(character);
    }

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & characterLayer.value) == 0) return;

        if (other.TryGetComponent<Character>(out Character character))
        {
            ApplyBuff(character);

            if (_growTreeIncreasesMaxHealth && !charactersInZone.Contains(character))
            {
                charactersInZone.Add(character);
                RpcAddCharacter(character.netId);

                if (_routine == null)
                    _routine = StartCoroutine(ApplyPartialShadowTreePeriodically());
            }
        }
    }

    [ServerCallback]
    private void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & characterLayer.value) == 0) return;

        if (other.TryGetComponent<Character>(out Character character))
        {
            RemoveBuff(character);

            if (_growTreeIncreasesMaxHealth)
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
    }

    private IEnumerator ApplyPartialShadowTreePeriodically()
    {
        var wait = new WaitForSeconds(_tick);

        while (charactersInZone.Count > 0)
        {
            foreach (Character character in charactersInZone)
            {
                if (character == null || !character.TryGetComponent(out CharacterState state)) continue;
                state.AddState(States.ShadowTree, 9999, 0f, gameObject, name);
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
            (state.GetState(States.ShadowTree) as ShadowTree)?.SwitchToFinite();
        }
    }
}
