using System.Collections;
using System.Linq;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;

public class CloneSelfSkill : Skill
{
    [Header("Clone Settings")]
    [SerializeField] private int _clonePrefabIndex = 0;
    [SerializeField] private float _spawnOffset = 2f;
    [SerializeField] private float _cloneDuration = 7f;
    
    private Character _activeClone;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => true;

    protected override IEnumerator CastJob()
    {
        var talentTypes = _hero.Abilities.Skills
            .Where(s => s.IsSkillActive && s.IsTalentSpell)
            .Take(3)
            .Select(s => s.GetType().AssemblyQualifiedName)
            .ToArray();

        Vector3 spawnPos = transform.position + transform.right * _spawnOffset;
        Quaternion spawnRot = transform.rotation;

        CmdSpawnClone(spawnPos, spawnRot, Hero.Health.CurrentValue, Hero.Health.MaxValue, talentTypes);

        yield return null;
    }

    [Command]
    private void CmdSpawnClone(Vector3 spawnPos, Quaternion spawnRot, float currentHp, float maxHp, string[] talentTypeNames)
    {
        var spawnComp = Hero.SpawnComponent;
        
        if (_activeClone != null)
            spawnComp.RemoveUnitServer(_activeClone);

        var prefabs = spawnComp.GetClonePrefabs();
        if (_clonePrefabIndex >= prefabs.Count) return;

        var cloneGO = Instantiate(prefabs[_clonePrefabIndex], spawnPos, spawnRot);

        var clone = cloneGO.GetComponent<Character>();
        if (clone == null) { Destroy(cloneGO); return; }

        clone.Initialize();
        clone.NetworkSettings.MyRoom = Hero.NetworkSettings.MyRoom;
        clone.NetworkSettings.TeamIndex = Hero.NetworkSettings.TeamIndex;
        clone.CharacterParent = Hero;

        NetworkServer.Spawn(cloneGO.gameObject, connectionToClient);

        spawnComp.AddUnit(clone);
        _activeClone = clone;

        ApplyCloneHealth(clone, currentHp, maxHp);
        RpcSetupCloneSkills(clone.gameObject, talentTypeNames);
        RpcResetEnemyFocus();
        StartCoroutine(DespawnAfter(cloneGO.gameObject, _cloneDuration));
    }

    private void ApplyCloneHealth(Character clone, float currentHp, float maxHp)
    {
        float delta = maxHp - clone.Health.MaxValue;
        if (Mathf.Abs(delta) > 0.01f)
            clone.Health.AddMax(delta);

        clone.Health.CurrentValue = currentHp;
    }

    [ClientRpc]
    private void RpcSetupCloneSkills(GameObject clone, string[] talentTypeNames)
    {
        if(clone == null) return;

        var characterClone = clone.GetComponent<Character>();
        
        var mgr = characterClone.Abilities;

        foreach (var skill in mgr.Skills.ToList())
        {
            mgr.DeactivateSkill(skill);
        }

        mgr.ActivateSkill(mgr.GetSkill<SpellMoveTo>());
        foreach (var typeName in talentTypeNames)
        {
            var type = System.Type.GetType(typeName);
            if (type == null) continue;

            var match = mgr.Skills.FirstOrDefault(s => s.GetType() == type);
            if (match != null)
            {
                mgr.ActivateSkill(match);
            }
        }
    }

    [ClientRpc]
    private void RpcResetEnemyFocus()
    {

    }

    private IEnumerator DespawnAfter(GameObject cloneGO, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (cloneGO != null && _activeClone != null)
        {
            Hero.SpawnComponent.RemoveUnitServer(_activeClone);
            _activeClone = null;
        }
    }

    protected override IEnumerator PrepareJob(System.Action<TargetInfo> callbackDataSaved)
    {
        callbackDataSaved(new TargetInfo());
        yield return null;
    }

    public override void LoadTargetData(TargetInfo targetInfo) { }
    protected override void ClearData() { }
}
