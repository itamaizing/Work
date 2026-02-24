using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MucusAutoGrowth : Skill, IPassiveSkill
{
    [SerializeField] private List<Transform> points;
    [SerializeField] private GameObject mucusPrefab;

    private const float TickRate = 1f;
    private const int MaxCircles = 6;

    private List<List<GameObject>> _mucusByCircle = new();
    private Coroutine _spawnRoutine;

    private float _timer = 0f;
    private float _remaining = 0f;
    private bool _infinite = true;
    private bool _isWombSpreadsMucus;

    private int _currentCircleIndex = 0;

    public static event Action OnAnyMucusAutoGrowthDestroyed;

    public bool IsWombSpreadsMucus 
    {
        get
        {
            return _isWombSpreadsMucus;
        }

        set
        {
            if (_isWombSpreadsMucus)
            {
                if (_spawnRoutine != null) _spawnRoutine = null;
                _spawnRoutine = StartCoroutine(ApplyMucusPeriodically());
            }

            else
            {
                StopCoroutine(ApplyMucusPeriodically());
                _spawnRoutine = null;
            }
        }
    }

    private void OnEnable()
    {
        _mucusByCircle.Clear();
        for (int i = 0; i < MaxCircles; i++)
            _mucusByCircle.Add(new List<GameObject>());

        Radius = 0;
    }

    private void OnDestroy()
    {
        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }

        CleanupAllMucus();
        OnAnyMucusAutoGrowthDestroyed?.Invoke();
    }
    private void OnDisable()
    {
        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }

        if (!gameObject.scene.isLoaded)
        {
            CleanupAllMucus();
        }
    }

    public void SwitchToFinite()
    {
        _infinite = false;
        _remaining = Mathf.Clamp(GetCurrentCircle(), 1, 9999);
        _timer = 0f;
    }

    public float RemainingDuration => _infinite ? 9999f : _remaining;

    private IEnumerator ApplyMucusPeriodically()
    {
        while (_infinite || _remaining > 0)
        {
            yield return new WaitForSeconds(TickRate);

            Radius = Mathf.Min(Radius + 1, 6);

            if (!_infinite)
            {
                _timer += TickRate;
                _remaining--;

                if (_remaining <= 0)
                {
                    CleanupAllMucus();
                    yield break;
                }
            }

            bool isNewCircleStarted = false;

            if (_currentCircleIndex < MaxCircles && _currentCircleIndex < points.Count)
            {
                Transform parent = points[_currentCircleIndex];
                if (parent != null)
                {
                    foreach (Transform point in parent)
                    {
                        if (point != null) CmdSpawnOrActivateMucus(point.position, _currentCircleIndex);
                    }

                    isNewCircleStarted = true;
                }

                _currentCircleIndex++;
            }
            else
            {
                for (int i = 0; i < _mucusByCircle.Count; i++)
                {
                    Transform parent = points.Count > i ? points[i] : null;
                    if (parent == null) continue;

                    int j = 0;
                    foreach (Transform point in parent)
                    {
                        if (point == null) continue;

                        if (j < _mucusByCircle[i].Count)
                        {
                            GameObject obj = _mucusByCircle[i][j];
                            if (obj != null)
                            {
                                if (!obj.activeSelf)
                                {
                                    if (obj.TryGetComponent<ObjectHealth>(out ObjectHealth objectHealth))
                                    {
                                        CmdSetCurrentHealth(objectHealth);
                                        obj.SetActive(true);
                                        CmdStartCustomRegeneration(objectHealth);
                                    }
                                }
                            }
                        }

                        j++;
                    }
                }
            }

            if (isNewCircleStarted && isServer)
            {
                var allMucus = FindObjectsOfType<Mucus>();
                foreach (var mucus in allMucus)
                {
                    if (mucus == null || mucus.MucusAutoGrowths.Contains(this)) continue;

                    float distance = Vector3.Distance(transform.position, mucus.transform.position);
                    if (distance > Radius) continue;

                    var health = mucus.GetComponent<ObjectHealth>();
                    if (health == null) continue;

                    health.ÑmdStartCustomRegeneration();
                    mucus.AddMucusAutoGrowth(this);

                    int circleIndex = Mathf.Clamp(_currentCircleIndex - 1, 0, _mucusByCircle.Count - 1);
                    if (!_mucusByCircle[circleIndex].Contains(mucus.gameObject)) _mucusByCircle[circleIndex].Add(mucus.gameObject);
                }
            }
        }
    }

    private int GetCurrentCircle()
    {
        for (int i = 0; i < _mucusByCircle.Count; i++) if (_mucusByCircle[i].Count == 0) return i;
        return _mucusByCircle.Count;
    }

    [Server]
    private void CmdSpawnOrActivateMucus(Vector3 spawnPosition, int circleIndex)
    {
        GameObject instance = Instantiate(mucusPrefab, spawnPosition, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(instance, Hero.NetworkSettings.MyRoom);
        NetworkServer.Spawn(instance, connectionToClient);

        RpcRegenerationEnabled(instance);
        if (instance.TryGetComponent<ObjectHealth>(out var objectHealth)) objectHealth.IsRegenerationEnabled = true;

        RpcSetMucusAutoGrowth(instance);
        if (instance.TryGetComponent<Mucus>(out var mucus))
        {
            mucus.AddMucusAutoGrowth(this);
        }

        uint netId = instance.GetComponent<NetworkIdentity>().netId;
        RpcAddMucus(netId, circleIndex);
    }

    [ClientRpc]
    private void RpcAddMucus(uint netId, int circleIndex)
    {
        if (NetworkClient.spawned.TryGetValue(netId, out var identity))
        {
            GameObject mucus = identity.gameObject;
            if (circleIndex < _mucusByCircle.Count) _mucusByCircle[circleIndex].Add(mucus);
        }
    }

    [ClientRpc]
    private void RpcRegenerationEnabled(GameObject instance)
    {
        if (instance.TryGetComponent<ObjectHealth>(out var objectHealth)) objectHealth.IsRegenerationEnabled = true;
    }

    [ClientRpc]
    private void RpcSetMucusAutoGrowth(GameObject instance)
    {
        if (instance.TryGetComponent<Mucus>(out var mucus)) mucus.AddMucusAutoGrowth(this);
    }

    [Command] private void CmdSetCurrentHealth(ObjectHealth objectHealth) => objectHealth.ServerSetCurrentHealth(5);
    [Command] private void CmdStartCustomRegeneration(ObjectHealth objectHealth) => objectHealth.ClientRpcStartCustomRegeneration();

    private void CleanupAllMucus()
    {
        foreach (var list in _mucusByCircle) list.Clear();
    }

    #region NotUsedSkillOverrides
    public override void LoadTargetData(TargetInfo targetInfo) { }
    protected override IEnumerator CastJob() => null;
    protected override void ClearData() { }
    protected override IEnumerator PrepareJob(System.Action<TargetInfo> callback) => null;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => false;
    #endregion
}
