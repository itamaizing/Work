using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinionCamp : NetworkBehaviour
{
    [SerializeField] private MinionLead _minionLeadPref;
    [SerializeField] private List<MinionComponent> _minionPrefs;
    [SerializeField, Range(0, 1)] private float _percentageHPForSurrender;

    private float _spawnDelay = 3;
    private float _distanceToLead = 3;
    private float _randomSpawnDistance = 3;

    private Coroutine _spawnCoroutine;
    private Coroutine _checkSurrenderCoroutine;
    private MinionLead _minionLead;
    private List<MinionComponent> _minions;
    private NetworkConnectionToClient _owner;
    private float _totalMaxHP;

    public event Action ReadyForSurrender;
    public event Action Surrendered;

    public override void OnStartServer()
    {
        base.OnStartServer();

        StartSpawnCoroutine();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        StartCheckSurrender();
    }

    public void StartSpawnCoroutine()
    {
        _spawnCoroutine = StartCoroutine(SpawnJob());
    }

    public void StartCheckSurrender()
    {
        _checkSurrenderCoroutine = StartCoroutine(CheckSurrenderJob());
    }

    private void Spawn()
    {
        if (_minionLead == null && _minions.Count <= 0)
        {
            var spawnPoint = new Vector3(UnityEngine.Random.Range(0, _randomSpawnDistance), UnityEngine.Random.Range(0, _randomSpawnDistance), UnityEngine.Random.Range(0, _randomSpawnDistance));
            _minionLead = Instantiate(_minionLeadPref, transform.position + spawnPoint, Quaternion.identity);
            NetworkServer.Spawn(_minionLead.gameObject);

            foreach (var item in _minionPrefs)
            {
                spawnPoint = new Vector3(UnityEngine.Random.Range(0, _randomSpawnDistance), UnityEngine.Random.Range(0, _randomSpawnDistance), UnityEngine.Random.Range(0, _randomSpawnDistance));
                var tempMinion = Instantiate(_minionLead, transform.position + spawnPoint, Quaternion.identity);
                _minions.Add(tempMinion);
                NetworkServer.Spawn(tempMinion.gameObject);
            }
        }
        else if (_minionLead != null && Vector3.Distance(_minionLead.transform.position, transform.position) <= _distanceToLead && _minions.Count <= 0)
        {
            foreach (var item in _minionPrefs)
            {
                var spawnPoint = new Vector3(UnityEngine.Random.Range(0, _randomSpawnDistance), UnityEngine.Random.Range(0, _randomSpawnDistance), UnityEngine.Random.Range(0, _randomSpawnDistance));
                var tempMinion = Instantiate(_minionLead, transform.position + spawnPoint, Quaternion.identity);
                _minions.Add(tempMinion);
                NetworkServer.Spawn(tempMinion.gameObject);

                _minionLead.SpawnComponent.AddUnit(tempMinion);

                if(_minionLead.netIdentity.connectionToClient != null)
                {
                    tempMinion.SetAuthority(_minionLead.netIdentity.connectionToClient);
                }
            }
        }
    }

    private float GetTotalHP()
    {
        if(_minionLead == null && _minions.Count <= 0)
        {
            return 0;
        }

        float totalHP = _minionLead.Health.CurrentValue;

        foreach (var item in _minions)
        {
            totalHP += item.Health.CurrentValue;
        }

        return totalHP;
    }

    private IEnumerator SpawnJob()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(_spawnDelay);
            Spawn();
        }
    }

    private IEnumerator CheckSurrenderJob()
    {
        yield return new WaitForSecondsRealtime(_spawnDelay + 1);

        _totalMaxHP = _minionLead.Health.MaxValue;

        foreach (var item in _minions)
        {
            _totalMaxHP += item.Health.MaxValue;
        }

        while (GetTotalHP() > (_totalMaxHP * (1 - _percentageHPForSurrender)))
        {
            yield return null;
        }

        ReadyForSurrender?.Invoke();
    }
}
