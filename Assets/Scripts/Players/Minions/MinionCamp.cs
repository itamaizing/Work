using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class MinionCamp : NetworkBehaviour
{
    [SerializeField] private MinionComponent _minionLeadPref;
    [SerializeField] private CampSurrenderUI _surrenderUI;
    [SerializeField] private List<MinionComponent> _minionPrefs;
    [SerializeField, Range(0, 1)] private float _percentageHPForSurrender;
    [SerializeField, Range(0, 10)] private float _distance = 5;

    private float _spawnDelayMinions = 15f;
    private float _spawnDelayForLead = 15f;
    private float _distanceToLead = 3;
    private float _randomSpawnDistance = 3;

    public CampStatus _campStatus = CampStatus.Neutral;

    private List<Character> _players = new();
    
    private Coroutine _spawnCoroutine;
    private Coroutine _checkSurrenderCoroutine;
    private MinionComponent _minionLead = null;
    private List<MinionComponent> _minions = new();
    public HashSet<HeroComponent> _attackers = new();
    private Dictionary<HeroComponent, Coroutine> _attackerTimers = new();
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

    public void SetPlayers(GameObject playersObject)
    {
        var playerSettings = playersObject.GetComponent<Character>();
        if (playerSettings != null)
        {
            _players.Add(playerSettings);
        }
    }

    public void StartSpawnCoroutine()
    {
        _spawnCoroutine = StartCoroutine(SpawnJob());
    }

    public void StartCheckSurrender()
    {
        _checkSurrenderCoroutine = StartCoroutine(CheckSurrenderJob());
    }

    public void AddAttacker(GameObject attacker)
    {
        if (attacker == null) return;

        if (attacker.TryGetComponent(out HeroComponent heroComponent))
        {

            if (!isServer)
            {
                CmdRefreshAttackersCount(attacker);
                return;
            }

            if (_attackers.Contains(heroComponent))
            {
                if (_attackerTimers.TryGetValue(heroComponent, out var existingCoroutine))
                {
                    StopCoroutine(existingCoroutine);
                    _attackerTimers.Remove(heroComponent);
                }
            }
            else
            {
                _attackers.Add(heroComponent);
                heroComponent.Health.HealTaked += TryAddHealer;
            }

            _attackerTimers[heroComponent] = StartCoroutine(RemoveAttackerAfterDelay(attacker, 5f));
        }
    }

    private void TryAddHealer(float amount, Skill skill, string skillName)
    {
        if (skill.Hero.gameObject == null) return;

        AddAttacker(skill.Hero.gameObject);
    }


    [Command]
    private void CmdRefreshAttackersCount(GameObject target)
    {
        AddAttacker(target);
    }

    private IEnumerator RemoveAttackerAfterDelay(GameObject hero, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (hero == null) yield break;

        if (hero.TryGetComponent(out HeroComponent heroComponent))
        {
            heroComponent.Health.HealTaked -= TryAddHealer;
            _attackers.Remove(heroComponent);
            _attackerTimers.Remove(heroComponent);
        }
    }

    private void Spawn()
    {
        if (_minionLead == null && _minions.Count <= 0)
        {
            var spawnPoint = new Vector3(UnityEngine.Random.Range(0, _randomSpawnDistance),
                UnityEngine.Random.Range(0, _randomSpawnDistance), UnityEngine.Random.Range(0, _randomSpawnDistance));
            _minionLead = Instantiate(_minionLeadPref, transform.position + spawnPoint, Quaternion.identity);
            NetworkServer.Spawn(_minionLead.gameObject);
            RpcAddMinionLead(_minionLead.gameObject);

            foreach (var item in _minionPrefs)
            {
                spawnPoint = new Vector3(UnityEngine.Random.Range(0, _randomSpawnDistance),
                    UnityEngine.Random.Range(0, _randomSpawnDistance),
                    UnityEngine.Random.Range(0, _randomSpawnDistance));
                var tempMinion = Instantiate(item, transform.position + spawnPoint, Quaternion.identity);
                _minions.Add(tempMinion);
                tempMinion.MyCamp = this;
                NetworkServer.Spawn(tempMinion.gameObject);
                RpcAddMinion(tempMinion.gameObject);
            }
        }
        else if (_minionLead != null &&
                 Vector3.Distance(_minionLead.transform.position, transform.position) <= _distanceToLead &&
                 _minions.Count <= 0)
        {
            foreach (var item in _minionPrefs)
            {
                var spawnPoint = new Vector3(UnityEngine.Random.Range(0, _randomSpawnDistance),
                    UnityEngine.Random.Range(0, _randomSpawnDistance),
                    UnityEngine.Random.Range(0, _randomSpawnDistance));
                var tempMinion = Instantiate(item, transform.position + spawnPoint, Quaternion.identity);
                _minions.Add(tempMinion);
                NetworkServer.Spawn(tempMinion.gameObject);

                if (_minionLead.netIdentity.connectionToClient != null)
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
                yield return new WaitForSecondsRealtime(1);
            }

            foreach (var hero in _attackers)
            {
                if (hero == null) continue;

                TargetShowSurrenderUI(hero.netIdentity.connectionToClient);
            }
            foreach (var timers in _attackerTimers.Values)
            {
                StopCoroutine(timers);
            }

            ReadyForSurrender?.Invoke();

            yield return new WaitForSecondsRealtime(1);
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
    
    
    [ClientRpc]
    private void RpcHideSurrenderUI()
    {
        if (_surrenderUI == null)
            return;

        _surrenderUI.Hide();
    }


    [Command(requiresAuthority = false)]
    public void CmdOnCapture(bool isTakeLead, NetworkConnectionToClient senderConn = null)
    {
        HeroComponent clickedHero = FindHeroByConnection(senderConn);
        
        if (clickedHero == null)
        {
            return;
        }
        
        int teamIndex = clickedHero.NetworkSettings.TeamIndex;
        
        if (isTakeLead)
        {
            _campStatus = teamIndex == 1 ? CampStatus.DarkTeam : CampStatus.LightTeam;

            TransferMinionsToHero(clickedHero);
        }
        
        _attackerTimers.Clear();
        _attackers.Clear();
        
        RpcHideSurrenderUI();

        UpdateMinionsLayersForAllPlayers();
        
        Surrendered?.Invoke();
    }
    
    private HeroComponent FindHeroByConnection(NetworkConnectionToClient conn)
    {
        foreach (var hero in _attackers)
        {
            if (hero?.netIdentity?.connectionToClient == conn)
                return hero;
        }
        return null;
    }

    [ClientRpc]
    private void RpcAddMinion(GameObject minion)
    {
        var tempMinion = minion.GetComponent<MinionComponent>();
        _minions.Add(tempMinion);
        tempMinion.MyCamp = this;
        
        foreach (var player in _players)
        {
            if (player == null) continue;

            var hero = player.GetComponent<HeroComponent>();
            if (hero == null || hero.netIdentity == null || hero.netIdentity.connectionToClient == null)
            {
                int localTeamIndex = hero.NetworkSettings.TeamIndex;
                SetMinionLayerForClient(minion, localTeamIndex);
            }
        }
    }


    [ClientRpc]
    private void RpcAddMinionLead(GameObject minion)
    {
        var tempMinion = minion.GetComponent<MinionComponent>();
        _minionLead = tempMinion;

        foreach (var player in _players)
        {
            if (player == null) continue;

            var hero = player.GetComponent<HeroComponent>();
            if (hero == null || hero.netIdentity == null || hero.netIdentity.connectionToClient == null)
            {
                int localTeamIndex = hero.NetworkSettings.TeamIndex;
                SetMinionLayerForClient(minion, localTeamIndex);
            }
        }
    }

    private void SetMinionLayerForClient(GameObject minion, int clientTeamIndex)
    {
        string layerName;
        
        switch (_campStatus)
        {
            case CampStatus.Neutral:
                layerName = "Enemy";
                break;
                
            case CampStatus.LightTeam:
                if (clientTeamIndex == 2)
                {
                    layerName = "Allies";
                }
                else
                {
                    layerName = "Enemy";
                }
                break;
                
            case CampStatus.DarkTeam:
                if (clientTeamIndex == 1)
                {
                    layerName = "Allies";
                }
                else
                {
                    layerName = "Enemy";
                }
                break;
                
            default:
                layerName = "Enemy";
                break;
        }
        
        minion.layer = LayerMask.NameToLayer(layerName);
    }
    private void UpdateMinionsLayersForAllPlayers()
    {
        if (_players == null || _players.Count == 0)
        {
            Debug.LogWarning("Players list is empty! Cannot update minion layers.");
            return;
        }

        foreach (var player in _players)
        {
            if (player == null) continue;

            var hero = player.GetComponent<HeroComponent>();
            if (hero == null || hero.netIdentity == null || hero.netIdentity.connectionToClient == null)
            {
                continue;
            }

            int playerTeamIndex = hero.NetworkSettings.TeamIndex;
            NetworkConnectionToClient conn = hero.netIdentity.connectionToClient;

            TargetUpdateMinionsLayers(conn, playerTeamIndex);
        }
    }
    
    [TargetRpc]
    private void TargetUpdateMinionsLayers(NetworkConnectionToClient conn, int clientTeamIndex)
    {
        if (_minionLead != null)
        {
            SetMinionLayerForClient(_minionLead.gameObject, clientTeamIndex);
        }

        foreach (var minion in _minions)
        {
            if (minion != null)
            {
                SetMinionLayerForClient(minion.gameObject, clientTeamIndex);
            }
        }
        
        Debug.Log($"[Client] Updated minion layers for team {clientTeamIndex}. Camp status: {_campStatus}");
    }
    
    private void TransferMinionsToHero(HeroComponent hero)
    {
        if (hero == null || hero.netIdentity == null || hero.netIdentity.connectionToClient == null)
        {
            Debug.LogError("Cannot transfer minions - invalid hero or connection");
            return;
        }

        if (_minionLead != null)
        {
            _minionLead.SetAuthority(hero.netIdentity.connectionToClient);
            hero.SpawnComponent.AddUnit(_minionLead);
        }

        foreach (var minion in _minions)
        {
            if (minion != null)
            {
                minion.SetAuthority(hero.netIdentity.connectionToClient);
                hero.SpawnComponent.AddUnit(minion);
            }
        }
    }

    public void RemoveDeadMinion(MinionComponent minion)
    {
        if (minion == _minionLead) _minionLead = null;
        _minions.Remove(minion);
    }
}

public enum CampStatus
{
    Neutral,
    DarkTeam,
    LightTeam
}
