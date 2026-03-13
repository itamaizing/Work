using Mirror;
using System.Collections;
using UnityEngine;

public class CampSpawner : NetworkBehaviour
{
    [SerializeField] private MinionComponent _minionLeadPref;
    [SerializeField] private MinionComponent[] _minionPrefs;
    [SerializeField, Range(0, 10)] private float _randomSpawnDistance = 3f;
    [SerializeField] private float _spawnDelaySeconds = 30f;
    [SerializeField] private float _distanceToLead = 3f;
    [SerializeField] private int _initialMinionCount = 3;

    private Coroutine _spawnCoroutine;
    private Transform _campTransform;
    private CampMinionManager _minionManager;
    private CampStatusController _statusController;
    
    public int InitialMinionCount => _initialMinionCount;
    private float _spawnTimer = 0f;
    private bool _isWaitingToSpawn = false;

    public void Initialize(Transform campTransform, CampMinionManager minionManager, CampStatusController statusController)
    {
        _campTransform = campTransform;
        _minionManager = minionManager;
        _statusController = statusController;
    }

    public void StartSpawning()
    {
        if (_spawnCoroutine == null)
        {
            _spawnCoroutine = StartCoroutine(SpawnJob());
        }
    }

    public void StopSpawning()
    {
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }
    }

    private IEnumerator SpawnJob()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(1f);

            CampStatus status = _statusController.CurrentStatus;
            bool isLeadTaken = _statusController.IsLeadTaken;
            var lead = _minionManager.GetLead();
            var minions = _minionManager.GetMinions();

            bool needsSpawn = false;

            if (status == CampStatus.Neutral)
            {
                if (lead == null || minions.Count == 0)
                {
                    needsSpawn = true;
                }
                else if (lead != null && 
                         Vector3.Distance(lead.transform.position, _campTransform.position) <= _distanceToLead &&
                         minions.Count < _initialMinionCount)
                {
                    needsSpawn = true;
                }
            }
            else if (!isLeadTaken && status != CampStatus.Neutral)
            {
                if (minions.Count < _initialMinionCount)
                {
                    needsSpawn = true;
                }
            }
            
            if (needsSpawn)
            {
                if (!_isWaitingToSpawn)
                {
                    _isWaitingToSpawn = true;
                    _spawnTimer = 0f;
                }

                _spawnTimer += 1f;
                
                if (_spawnTimer >= _spawnDelaySeconds)
                {
                    if (status == CampStatus.Neutral)
                    {
                        SpawnNeutralMinions();
                    }
                    else if (!isLeadTaken && status != CampStatus.Neutral)
                    {
                        SpawnControlledMinions();
                    }

                    ResetSpawnTimer();
                }
            }
            else
            {
                ResetSpawnTimer();
            }
        }
    }

    private void SpawnNeutralMinions()
    {
        var lead = _minionManager.GetLead();
        var minions = _minionManager.GetMinions();

        if (lead == null && minions.Count == 0)
        {
            SpawnLead();
            SpawnAllMinions(_initialMinionCount);
        }
        else if (lead != null &&
                 Vector3.Distance(lead.transform.position, _campTransform.position) <= _distanceToLead &&
                 minions.Count < _initialMinionCount)
        {
            int missingCount = _initialMinionCount - minions.Count;
            SpawnMinions(minions.Count, missingCount);
        }
    }

    private void SpawnControlledMinions()
    {
        var owner = _statusController.CurrentOwner;
        if (owner == null || _statusController.CurrentStatus == CampStatus.Neutral || _statusController.IsLeadTaken)
            return;

        var minions = _minionManager.GetMinions();
        int missingCount = _initialMinionCount - minions.Count;

        if (missingCount > 0)
        {
            SpawnMinionsForOwner(minions.Count, missingCount, owner);
        }
    }

    public MinionComponent SpawnLead()
    {
        Vector3 spawnPoint = GetRandomSpawnPoint();
        var lead = Instantiate(_minionLeadPref, _campTransform.position + spawnPoint, Quaternion.identity);
        lead.Initialize();
        NetworkServer.Spawn(lead.gameObject);

        _minionManager.SetLead(lead);
        
        return lead;
    }

    private void SpawnAllMinions(int count)
    {
        for (int i = 0; i < count && i < _minionPrefs.Length; i++)
        {
            SpawnMinion(i);
        }
    }

    private void SpawnMinions(int startIndex, int count)
    {
        for (int i = 0; i < count; i++)
        {
            int prefabIndex = startIndex + i;
            if (prefabIndex >= _minionPrefs.Length) break;

            SpawnMinion(prefabIndex);
        }
    }

    private void SpawnMinionsForOwner(int startIndex, int count, Character owner)
    {
        for (int i = 0; i < count; i++)
        {
            int prefabIndex = startIndex + i;
            if (prefabIndex >= _minionPrefs.Length) break;

            var minion = SpawnMinion(prefabIndex);

            if (owner.netIdentity.connectionToClient != null)
            {
                minion.SetAuthority(owner.netIdentity.connectionToClient);
                owner.SpawnComponent.AddUnit(minion);
            }

            _minionManager.SetMinionLayerForOwner(minion.gameObject, owner.NetworkSettings.TeamIndex);
        }
    }

    private MinionComponent SpawnMinion(int prefabIndex)
    {
        Vector3 spawnPoint = GetRandomSpawnPoint();
        var minion = Instantiate(_minionPrefs[prefabIndex], _campTransform.position + spawnPoint, Quaternion.identity);
        minion.Initialize();
        NetworkServer.Spawn(minion.gameObject);

        _minionManager.AddMinion(minion);
        
        return minion;
    }

    public void FullRespawn()
    {
        _minionManager.ClearControlledMinions();
        
        _minionManager.ClearAllMinions();
        
        ResetSpawnTimer();
    }

    public void RespawnMissing()
    {
        if (_minionManager.GetLead() == null)
        {
            SpawnLead();
        }

        var minions = _minionManager.GetMinions();
        int missingCount = _initialMinionCount - minions.Count;

        if (missingCount > 0)
        {
            SpawnMinions(minions.Count, missingCount);
        }
        
        ResetSpawnTimer();
    }
    
    public void ResetSpawnTimer()
    {
        _spawnTimer = 0f;
        _isWaitingToSpawn = false;
    }

    private Vector3 GetRandomSpawnPoint()
    {
        return new Vector3(
            Random.Range(0, _randomSpawnDistance),
            Random.Range(0, _randomSpawnDistance),
            Random.Range(0, _randomSpawnDistance)
        );
    }
}
