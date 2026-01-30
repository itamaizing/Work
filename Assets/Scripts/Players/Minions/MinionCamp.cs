using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MinionCamp : NetworkBehaviour
{
    [SerializeField] private LayerMask _playersLayerMask;
    [SerializeField] private MinionComponent _minionLeadPref;
    [SerializeField] private CampSurrenderUI _surrenderUI;
    [SerializeField] private List<MinionComponent> _minionPrefs;
    [SerializeField, Range(0, 1)] private float _percentageHPForSurrender;
    [SerializeField, Range(0, 10)] private float _distance = 5;

    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private LayerMask _allyLayer;

    private float _spawnDelayMinions = 15f;
    private float _spawnDelayForLead = 15f;
    private float _distanceToLead = 3;
    private float _randomSpawnDistance = 3;

    public CampStatus _campStatus = CampStatus.Neutral;

    private Coroutine _spawnCoroutine;
    private Coroutine _checkSurrenderCoroutine;
    private MinionComponent _minionLead = null;
    private List<MinionComponent> _minions = new();
    public HashSet<NetworkConnectionToClient> _attackers = new();
    private Dictionary<NetworkConnectionToClient, Coroutine> _attackerTimers = new();
    private NetworkConnectionToClient _owner;
    public float _totalMaxHP;

    public event Action ReadyForSurrender;
    public event Action Surrendered;

    public override void OnStartServer()
    {
        base.OnStartServer();
        StartSpawnCoroutine();
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
    
    public void AddAttacker(NetworkConnectionToClient attacker)
    {
        if (attacker == null) return;

        if (!isServer)
        {
            CmdRefreshAttackersCount(attacker);
            return;
        }

        if (_attackers.Contains(attacker))
        {
            if (_attackerTimers.TryGetValue(attacker, out var existingCoroutine))
            {
                StopCoroutine(existingCoroutine);
                _attackerTimers.Remove(attacker);
            }
        }
        else
        {
            _attackers.Add(attacker);
        }

        _attackerTimers[attacker] = StartCoroutine(RemoveAttackerAfterDelay(attacker, 5f));
    }

    
    [Command]
    private void CmdRefreshAttackersCount(NetworkConnectionToClient target)
    {
        AddAttacker(target);  // This will run on server and execute the server logic
    }

    private IEnumerator RemoveAttackerAfterDelay(NetworkConnectionToClient hero, float delay)
    {
        yield return new WaitForSeconds(delay);

        _attackers.Remove(hero);
        _attackerTimers.Remove(hero);
    }
    private void Spawn()
    {
        if (_minionLead == null && _minions.Count <= 0)
        {
            var spawnPoint = new Vector3(UnityEngine.Random.Range(0, _randomSpawnDistance), UnityEngine.Random.Range(0, _randomSpawnDistance), UnityEngine.Random.Range(0, _randomSpawnDistance));
            _minionLead = Instantiate(_minionLeadPref, transform.position + spawnPoint, Quaternion.identity);
            NetworkServer.Spawn(_minionLead.gameObject);
            RpcAddMinionLead(_minionLead.gameObject);

            foreach (var item in _minionPrefs)
            {
                spawnPoint = new Vector3(UnityEngine.Random.Range(0, _randomSpawnDistance), UnityEngine.Random.Range(0, _randomSpawnDistance), UnityEngine.Random.Range(0, _randomSpawnDistance));
                var tempMinion = Instantiate(item, transform.position + spawnPoint, Quaternion.identity);
                _minions.Add(tempMinion);
                tempMinion.MyCamp = this;
                NetworkServer.Spawn(tempMinion.gameObject);
                RpcAddMinion(tempMinion.gameObject);
            }
        }
        else if (_minionLead != null && Vector3.Distance(_minionLead.transform.position, transform.position) <= _distanceToLead && _minions.Count <= 0)
        {
            foreach (var item in _minionPrefs)
            {
                var spawnPoint = new Vector3(UnityEngine.Random.Range(0, _randomSpawnDistance), UnityEngine.Random.Range(0, _randomSpawnDistance), UnityEngine.Random.Range(0, _randomSpawnDistance));
                var tempMinion = Instantiate(item, transform.position + spawnPoint, Quaternion.identity);
                _minions.Add(tempMinion);
                NetworkServer.Spawn(tempMinion.gameObject);

                //_minionLead.SpawnComponent.AddUnit(tempMinion);

                if(_minionLead.netIdentity.connectionToClient != null)
                {
                    tempMinion.SetAuthority(_minionLead.netIdentity.connectionToClient);
                }
            }
        }
    }

    private float GetTotalHP()
    {
        float totalHP = 0;

        if (_minionLead == null && _minions.Count <= 0)
        {
            return 0;
        }

        if (_minionLead != null && Vector3.Distance(transform.position, _minionLead.transform.position) <= _distance)
        {
            totalHP = _minionLead.Health.CurrentValue;
        }
        

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
            yield return new WaitForSecondsRealtime(_spawnDelayMinions);
            Spawn();
        }
    }

    private IEnumerator CheckSurrenderJob()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(_spawnDelayMinions + 1);

            if (_minionLead != null)
            {
                _totalMaxHP = _minionLead.Health.MaxValue;
            }
            
            foreach (var item in _minions)
            {
                _totalMaxHP += item.Health.MaxValue;
            }
            while (GetTotalHP() > (_totalMaxHP * (1 - _percentageHPForSurrender)))
            {
                yield return new WaitForSecondsRealtime(1); ;
            }
            foreach (var hero in _attackers)
            {
                if (hero == null) continue;

                TargetShowSurrenderUI(hero);
            }

            ReadyForSurrender?.Invoke();

            //HeroComponent owner = null;

            /*while (owner == null)
            {
                owner = GetClosestOwner();
                yield return null;
            }*/

            /*if (_minionLead != null && _minionLead.IsIntercepted == false)
            {
                CmdIntercept(_minionLead.gameObject, owner.gameObject);
            }*/


            /*foreach (var item in _minions)
            {
                if(item.IsIntercepted == false)
                    CmdIntercept(item.gameObject, owner.gameObject);
            }*/
            yield return new WaitForSecondsRealtime(1); ;
        }
    }

    [TargetRpc]
    private void TargetShowSurrenderUI(NetworkConnectionToClient conn)
    {
        if (_surrenderUI == null)
            return;

        _surrenderUI.gameObject.SetActive(true);
        _surrenderUI.Setup(this);
        _surrenderUI.Show();
    }

    [Command(requiresAuthority = false)]
    public void CmdChooseOption1()
    {
        // Empty for now
    }

    [Command(requiresAuthority = false)]
    public void CmdChooseOption2()
    {
        // Empty for now
    }

    private HeroComponent GetClosestOwner()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, _distance, _playersLayerMask);

        Collider closest = null;
        float closestDistance = Mathf.Infinity;
        Vector3 currentPosition = transform.position;

        foreach (Collider col in colliders)
        {
            if (col == null) continue;

            float distance = Vector3.Distance(currentPosition, col.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = col;
            }
        }

        return closest.GetComponent<HeroComponent>();
    }

    [ClientRpc]
    private void RpcAddMinion(GameObject minion)
    {
        var tempMinion = minion.GetComponent<MinionComponent>();
        _minions.Add(tempMinion);
        tempMinion.MyCamp = this;
        SetMinionLayer(minion);
    }
    
    public void RemoveDeadMinion(MinionComponent minion)
    {
        if (minion == _minionLead) _minionLead = null;
        _minions.Remove(minion);
    }
    
    private void SetMinionLayer(GameObject minion)
    {
        switch (_campStatus)
        {
            case CampStatus.Neutral:
                minion.gameObject.layer = LayerMask.NameToLayer("Enemy");
                break;
            case CampStatus.DarkTeam:
                minion.gameObject.layer = LayerMask.NameToLayer("Enemy");
                break;
            case CampStatus.LightTeam:
                minion.gameObject.layer = LayerMask.NameToLayer("Enemy");
                break;
            default:
                minion.gameObject.layer = LayerMask.NameToLayer("Enemy");
                break;
        }
    }

    [ClientRpc]
    private void RpcAddMinionLead(GameObject minion)
    {
        var tempMinion = minion.GetComponent<MinionComponent>();
        _minionLead = tempMinion;
        SetMinionLayer(minion);
    }

    [Command(requiresAuthority = false)]
    private void CmdIntercept(GameObject minion, GameObject hero)
    {
        Debug.Log(minion.GetComponent<MinionComponent>().authority);
        minion.GetComponent<MinionComponent>().SetAuthority(hero.GetComponent<NetworkIdentity>().connectionToClient);
        Debug.Log(minion.GetComponent<MinionComponent>().authority);

        var player = hero.GetComponent<HeroComponent>();
        player.SpawnComponent.AddUnit(minion.GetComponent<MinionComponent>());
    }
}

public enum CampStatus
{
    Neutral,
    DarkTeam,
    LightTeam
}
