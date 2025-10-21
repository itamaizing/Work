using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class MucusAutoGrowth : Skill, IPassiveSkill
{
    public override void LoadTargetData(TargetInfo targetInfo) { }
    protected override IEnumerator CastJob() { yield break; }
    protected override void ClearData() { }
    protected override IEnumerator PrepareJob(System.Action<TargetInfo> targetDataSavedCallback) { yield break; }
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => false;

    [SerializeField] private List<GameObject> points;
    [SerializeField] private ObjectData mucusData;
    [SerializeField] private GameObject mucus;
    [SerializeField] private List<GameObject> _activeMucus = new();

    private GameObject _currentMucus;

    private Coroutine _activationRoutine;

    private void OnEnable()
    {
      ActivateMucus();
    }

    private void OnDisable()
    {
        if (_activationRoutine != null) StopCoroutine(_activationRoutine);
    }

    private void ActivateMucus() => _activationRoutine = StartCoroutine(ActivateMucusOverTime());

    private IEnumerator ActivateMucusOverTime()
    {
        foreach (var point in points)
        {
            yield return new WaitForSeconds(1f);

            if (point == null) continue;

            point.SetActive(true);

            foreach (Transform child in point.transform)
            {
                if (child != null && mucus != null)
                {
                    CmdSpawnMucus(child.gameObject);
                }
            }
        }
    }

    [Server]
    private void CmdSpawnMucus(GameObject point)
    {
        GameObject instance = Instantiate(mucus, point.transform.position, point.transform.rotation);
        SceneManager.MoveGameObjectToScene(instance, Hero.NetworkSettings.MyRoom);
        NetworkServer.Spawn(instance, connectionToClient);

        ObjectHealth health = instance.GetComponentInChildren<ObjectHealth>();
        if (health != null)
        {
            health.InitializeObject(mucusData);
            if (mucusData.MinEndurance) health.ServerStartFillHP(health.ObjectData.MaxHealth, 1f);
        }

        _currentMucus = instance;
        _activeMucus.Add(instance);

        uint netId = instance.GetComponent<NetworkIdentity>().netId;
        RpcClientAddMucus(netId);
    }

    [ClientRpc]
    private void RpcClientAddMucus(uint netId)
    {
        if (NetworkClient.spawned.TryGetValue(netId, out var networkIdentity))
        {
            GameObject mucus = networkIdentity.gameObject;
            _currentMucus = mucus;
            _activeMucus.Add(mucus);
        }
    }
}
