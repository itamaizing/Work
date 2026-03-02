using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SpawnType
{
    Scrader = 0,
    Spisnacider = 1,
    Getomir = 2,
}

public class CreatureSpawn : Skill
{
    private Vector3 _spawnPoint = Vector3.positiveInfinity;

    [SerializeField] private SpawnComponent spawnComponent;
    [SerializeField] private MinionMove minionMove;
    [SerializeField] private MinionComponent minion;
    [SerializeField] private Tentacles tentacle;

    [SerializeField] private SpawnType spawnType;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => _spawnPoint != Vector3.positiveInfinity;

    public Tentacles Tentacle { get => tentacle; set => tentacle = value; }

    private void OnEnable()
    {
        minionMove.SetCanMove(false);
    }

    private void OnDisable()
    {
        if (spawnType == SpawnType.Getomir && tentacle != null)
        {
            tentacle.OnSpawnGetomirChanged -= HandleSpawnGetomirChanged;
        }
    }

    private void Start()
    {
        if (spawnType == SpawnType.Getomir && tentacle != null)
        {
            tentacle.OnSpawnGetomirChanged += HandleSpawnGetomirChanged;
        }
    }

    private void HandleSpawnGetomirChanged(bool isActive)
    {
        Debug.Log("1");
        if (spawnType != SpawnType.Getomir) return;
        if (Hero == null) return;

        var skillManager = Hero.Abilities;
        if (skillManager == null) return;

        if (isActive) skillManager.ActivateSkill(this);
        else skillManager.DeactivateSkill(this);
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
            int index = (int)spawnType;
            spawnComponent.CmdSpawnAliesPoint(_spawnPoint, Quaternion.identity, minion, index, true, tentacle.Hero);
        }

        yield return null;
    }

    protected override void ClearData() { }
}
