using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class TimeBackSkill : Skill
{
    [SerializeField] private float _snapshotInterval = 0.1f;
    [SerializeField] private float _rewindTime = 3f;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    private struct CharacterSnapshot
    {
        public float Timestamp;
        public Vector3 Position;
        public Dictionary<ResourceType, float> ResourceValues;
    }

    private Queue<CharacterSnapshot> _snapshots = new();
    private Coroutine _recordCoroutine;

    public override void LoadTargetData(TargetInfo targetInfo) { }
    
    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        yield return null;
    }
    protected override void ClearData() { }
    
    public void StartRecording()
    {
        if (_recordCoroutine != null) return;
        _recordCoroutine = StartCoroutine(RecordCoroutine());
    }

    public void StopRecording()
    {
        if (_recordCoroutine != null)
        {
            StopCoroutine(_recordCoroutine);
            _recordCoroutine = null;
        }
        _snapshots.Clear();
    }

    private IEnumerator RecordCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(_snapshotInterval);

            if (_hero == null) continue;

            if (!isOwned) continue;

            var snapshot = new CharacterSnapshot
            {
                Timestamp = Time.time,
                Position = _hero.transform.position,
                ResourceValues = new Dictionary<ResourceType, float>()
            };

            foreach (var resource in _hero.Resources.Values)
                snapshot.ResourceValues[resource.Type] = resource.CurrentValue;

            _snapshots.Enqueue(snapshot);

            float cutoff = Time.time - _rewindTime - _snapshotInterval;
            while (_snapshots.Count > 0 && _snapshots.Peek().Timestamp < cutoff)
                _snapshots.Dequeue();
        }
    }

    protected override IEnumerator CastJob()
    {
        if (_snapshots.Count == 0)
            yield break;

        float targetTime = Time.time - _rewindTime;
        CharacterSnapshot best = default;
        float bestDiff = float.MaxValue;

        foreach (var snap in _snapshots)
        {
            float diff = Mathf.Abs(snap.Timestamp - targetTime);
            if (diff < bestDiff)
            {
                bestDiff = diff;
                best = snap;
            }
        }

        CmdApplyRewind(best.Position, SerializeResources(best.ResourceValues));
        yield return null;
    }

    private ResourceSnapshot[] SerializeResources(Dictionary<ResourceType, float> dict)
    {
        var arr = new ResourceSnapshot[dict.Count];
        int i = 0;
        foreach (var kvp in dict)
            arr[i++] = new ResourceSnapshot { Type = kvp.Key, Value = kvp.Value };
        return arr;
    }

    [Command]
    private void CmdApplyRewind(Vector3 position, ResourceSnapshot[] resources)
    {
        RpcApplyRewind(position, resources);
    }

    [ClientRpc]
    private void RpcApplyRewind(Vector3 position, ResourceSnapshot[] resources)
    {
        if (_hero == null) return;

        _hero.transform.position = position;
        _hero.Rigidbody.linearVelocity = Vector3.zero;

        if (!isOwned) return;

        foreach (var snap in resources)
        {
            var resource = _hero.TryGetResource(snap.Type);
            if (resource == null) continue;

            float current = resource.CurrentValue;
            float target = snap.Value;
            float delta = target - current;

            if (delta > 0)
                resource.CmdAdd(delta);
            else if (delta < 0)
                resource.CmdUse(-delta);
        }
    }
}

[Serializable]
public struct ResourceSnapshot
{
    public ResourceType Type;
    public float Value;
}
