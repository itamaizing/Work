using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ScraderSpawn : Skill
{
    private Vector3 _spawnPoint = Vector3.positiveInfinity;

    [SerializeField] private SpawnComponent spawnComponent;
    [SerializeField] private CocoonSpawn cocoonSpawn;
    [SerializeField] private MinionMove minionMove;
    [SerializeField] private MinionComponent minion;
    [SerializeField] private Tentacles tentacle;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => _spawnPoint != Vector3.positiveInfinity;

    public Tentacles Tentacle { get => tentacle; set => tentacle = value; }
    public CocoonSpawn CocoonSpawn { get => cocoonSpawn; set => cocoonSpawn = value; }

    private void OnEnable()
    {
        Hero.Move.CanMove = false;
    }

    private void OnDestroy()
    {
        if (cocoonSpawn != null) cocoonSpawn.CurrentCounter--;
    }

    private void Start()
    {
        if (cocoonSpawn != null) cocoonSpawn.CurrentCounter++;
    }

    protected override IEnumerator PrepareJob(System.Action<TargetInfo> callback)
    {
        TargetInfo info = new TargetInfo();
        info.Points.Add(transform.position);
        callback?.Invoke(info);

        yield break;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Points.Count > 0)
            _spawnPoint = targetInfo.Points[0];
    }

    protected override IEnumerator CastJob()
    {
        if (minion.TryGetComponent<Character>(out var character))
        {
            character.SelectComponent?.Deselect();
            character.SelectedCircle?.SwitchClostestTarget(false);
            character.SelectedCircle.gameObject.SetActive(false);

            if (character.TryGetComponent<MinimapMarker>(out var minimap)) minimap.IsActive = false;

            var states = new List<AbstractCharacterState>(character.CharacterState.CurrentStates);
            foreach (var state in states) character.CharacterState.RemoveState(state.State);
        }

        Hero.Abilities.DeactivateSkill(this);

        if (tentacle.TryGetComponent<SpawnComponent>(out var spawnComponent)) spawnComponent.CmdSpawnAliesPoint(_spawnPoint, Quaternion.identity, minion, 0, true , tentacle.Hero);

        yield return null;
    }

    protected override void ClearData() { }
}
