using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrowTreeAura : NetworkBehaviour
{
    [Header("Tick & Radius")]
    [SerializeField] private float _tick = 1f;
    [SerializeField] private float zoneRadius = 8f;
    [SerializeField] private LayerMask characterLayer;

    private readonly List<Character> _insideNow = new();
    private Coroutine _routine;

    private void Awake() => _routine = StartCoroutine(AuraRoutine());

    private void OnDestroy()
    {
        if (_routine != null) StopCoroutine(_routine);

        for (int i = _insideNow.Count - 1; i >= 0; --i)
            if (_insideNow[i].TryGetComponent(out CharacterState state)) (state.GetState(States.ShadowTree) as ShadowTree)?.SwitchToFinite();

        _insideNow.Clear();
    }

    private IEnumerator AuraRoutine()
    {
        var wait = new WaitForSeconds(_tick);

        while (true)
        {
            CmdApplyEffectsInZone();
            yield return wait;
        }
    }

    [Command] private void CmdApplyEffectsInZone() => ClientApplyEffectsInZone();

    [ClientRpc]
    private void ClientApplyEffectsInZone()
    {
        var current = new HashSet<Character>();
        Collider[] colliders = Physics.OverlapSphere(transform.position, zoneRadius, characterLayer);

        foreach (var collider in colliders)
        {
            if (!collider.TryGetComponent(out Character character)) continue;
            current.Add(character);

            if (!character.TryGetComponent(out CharacterState state)) continue;

            AddState(state);
        }

        for (int i = _insideNow.Count - 1; i >= 0; i--)
        {
            var character = _insideNow[i];
            if (current.Contains(character)) continue;

            if (character.TryGetComponent(out CharacterState state)) (state.GetState(States.ShadowTree) as ShadowTree)?.SwitchToFinite();

            _insideNow.RemoveAt(i);
        }

        _insideNow.Clear();
        _insideNow.AddRange(current);
    }

    private void AddState(CharacterState state) => state.AddStateLogic(States.ShadowTree, 9999, 0f, Schools.None, gameObject, name);
}
