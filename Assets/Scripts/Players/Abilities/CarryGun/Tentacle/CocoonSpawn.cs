using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CocoonSpawn : Skill
{
    private Vector3 _spawnPoint = Vector3.positiveInfinity;

    [SerializeField] private SpawnComponent spawnComponent;
    [SerializeField] private MinionMove minionMove;
    [SerializeField] private MinionComponent minion;
    [SerializeField] private Tentacles tentacle;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => _spawnPoint != Vector3.positiveInfinity;

    public Tentacles Tentacle { get => tentacle; set => tentacle = value; }

    protected override void Awake()
    {
        base.Awake();
        minionMove.CanMove = false;
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

        if (tentacle.TryGetComponent<SpawnComponent>(out var spawnComponent))
        {
            Vector3 forwardOffset = transform.forward.normalized * 1.6f;
            Vector3 spawnPos = transform.position + forwardOffset;
            spawnComponent.CmdSpawnEnemyPoint(spawnPos, Quaternion.identity, minion, 1, false);
        }

        CmdTentacleCocoon(spawnComponent);
         yield return null;
    }

    [Command]
    private void CmdTentacleCocoon(SpawnComponent spawnComponent)
    {
        RpcTentacleCocoon(spawnComponent);
    }


    [ClientRpc]
    private void RpcTentacleCocoon(SpawnComponent spawnComponent)
    {
        foreach (var cocoon in spawnComponent.Units) if (cocoon.TryGetComponent<ScraderSpawn>(out ScraderSpawn scraderSpawn)) scraderSpawn.Tentacle = tentacle;
    }

    protected override void ClearData() { }
}
