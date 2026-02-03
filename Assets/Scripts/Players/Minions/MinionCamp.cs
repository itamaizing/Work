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
    [SerializeField] private MeshRenderer _campColor;

    private float _spawnDelayMinions = 15f;
    private float _spawnDelayForLead = 15f;
    private float _distanceToLead = 3;
    private float _randomSpawnDistance = 3;

    public CampStatus _campStatus = CampStatus.Neutral;

    [SerializeField]private List<Character> _players = new();
   
    private Coroutine _spawnCoroutine;
    private Coroutine _checkSurrenderCoroutine;
    private Coroutine _checkNeutralCoroutine;
    private MinionComponent _minionLead = null;
    private List<MinionComponent> _minions = new();
    public HashSet<HeroComponent> _attackers = new();
    private Dictionary<HeroComponent, Coroutine> _attackerTimers = new();
    private NetworkConnectionToClient _owner;
    public float _totalMaxHP;

    private int _initialMinionCount = 1;
    private bool _isLeadTaken = false;
    private HeroComponent _currentOwner = null;
    
    public override void OnStartServer()
    {
        base.OnStartServer();
        StartSpawnCoroutine();
        StartCheckSurrender();
        StartCheckNeutral();
    }
   
    public void SetPlayers(GameObject playersObject)
    {
        var playerSettings = playersObject.GetComponent<Character>();
        if (playerSettings != null && playerSettings.NetworkSettings.isOwned)
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

    public void StartCheckNeutral()
    {
        _checkNeutralCoroutine = StartCoroutine(CheckNeutralJob());
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
            _minionLead.MyCamp = this;
            RpcAddMinionLead(_minionLead.gameObject);

            for (int i = 0; i < _initialMinionCount; i++)
            {
                if (i >= _minionPrefs.Count) break;
                
                spawnPoint = new Vector3(UnityEngine.Random.Range(0, _randomSpawnDistance),
                    UnityEngine.Random.Range(0, _randomSpawnDistance),
                    UnityEngine.Random.Range(0, _randomSpawnDistance));
                var tempMinion = Instantiate(_minionPrefs[i], transform.position + spawnPoint, Quaternion.identity);
                _minions.Add(tempMinion);
                tempMinion.MyCamp = this;
                NetworkServer.Spawn(tempMinion.gameObject);
                RpcAddMinion(tempMinion.gameObject);
            }
        }
        else if (_minionLead != null &&
                 Vector3.Distance(_minionLead.transform.position, transform.position) <= _distanceToLead &&
                 _minions.Count < _initialMinionCount && _campStatus == CampStatus.Neutral)
        {
            int minionsToSpawn = _initialMinionCount - _minions.Count;
            
            for (int i = 0; i < minionsToSpawn; i++)
            {
                if (i >= _minionPrefs.Count) break;
                
                var spawnPoint = new Vector3(UnityEngine.Random.Range(0, _randomSpawnDistance),
                    UnityEngine.Random.Range(0, _randomSpawnDistance),
                    UnityEngine.Random.Range(0, _randomSpawnDistance));
                var tempMinion = Instantiate(_minionPrefs[i], transform.position + spawnPoint, Quaternion.identity);
                _minions.Add(tempMinion);
                tempMinion.MyCamp = this;
                NetworkServer.Spawn(tempMinion.gameObject);

                if (_minionLead.netIdentity.connectionToClient != null)
                {
                    tempMinion.SetAuthority(_minionLead.netIdentity.connectionToClient);
                }
                
                RpcAddMinion(tempMinion.gameObject);
            }
        }
    }

    public void OnMinionDied(GameObject deadMinion)
    {
        if (!isServer) return;

        if (deadMinion == null) return;
        
        var minionComp = deadMinion.GetComponent<MinionComponent>();
        if (minionComp == null) return;

        if (minionComp == _minionLead)
        {
            _minionLead = null;
        }
        else
        {
            _minions.Remove(minionComp);
        }
    }

    private IEnumerator CheckNeutralJob()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(1f);

            if (_campStatus == CampStatus.Neutral) continue;

            bool shouldReturnToNeutral = false;

            if (_isLeadTaken)
            {
                if (_minions.Count == 0)
                {
                    shouldReturnToNeutral = true;
                }
            }
            else
            {
                if (_minionLead == null)
                {
                    shouldReturnToNeutral = true;
                }
            }

            if (shouldReturnToNeutral)
            {
                ReturnCampToNeutral();
            }
        }
    }

    private void ReturnCampToNeutral()
    {
        bool wasLeadLeft = !_isLeadTaken;
        
        _campStatus = CampStatus.Neutral;
        _isLeadTaken = false;
        _currentOwner = null;
        
        SetCampStatus(_campStatus);

        if (wasLeadLeft && _minionLead == null)
        {
            FullRespawnCamp();
        }
        else
        {
            RespawnCamp();
        }
        
        UpdateMinionsLayersForAllPlayers();
    }

    private void RespawnCamp()
    {
        if (_minionLead == null)
        {
            SpawnNewLead();
        }

        int currentMinionCount = _minions.Count;
        int minionsToSpawn = _initialMinionCount - currentMinionCount;

        for (int i = 0; i < minionsToSpawn && i < _minionPrefs.Count; i++)
        {
            var spawnPoint = new Vector3(UnityEngine.Random.Range(0, _randomSpawnDistance),
                UnityEngine.Random.Range(0, _randomSpawnDistance),
                UnityEngine.Random.Range(0, _randomSpawnDistance));
            var tempMinion = Instantiate(_minionPrefs[i], transform.position + spawnPoint, Quaternion.identity);
            _minions.Add(tempMinion);
            tempMinion.MyCamp = this;
            NetworkServer.Spawn(tempMinion.gameObject);
            RpcAddMinion(tempMinion.gameObject);
        }
    }
    
    private void SpawnControlledMinions()
    {
        if (_currentOwner == null || _campStatus == CampStatus.Neutral || _isLeadTaken) return;

        int currentMinionCount = _minions.Count;
        int minionsToSpawn = _initialMinionCount - currentMinionCount;

        for (int i = 0; i < minionsToSpawn && i < _minionPrefs.Count; i++)
        {
            var spawnPoint = new Vector3(UnityEngine.Random.Range(0, _randomSpawnDistance),
                UnityEngine.Random.Range(0, _randomSpawnDistance),
                UnityEngine.Random.Range(0, _randomSpawnDistance));
            var tempMinion = Instantiate(_minionPrefs[i], transform.position + spawnPoint, Quaternion.identity);
            _minions.Add(tempMinion);
            tempMinion.MyCamp = this;
            NetworkServer.Spawn(tempMinion.gameObject);

            if (_currentOwner.netIdentity.connectionToClient != null)
            {
                tempMinion.SetAuthority(_currentOwner.netIdentity.connectionToClient);
                _currentOwner.SpawnComponent.AddUnit(tempMinion);
            }

            int ownerTeamIndex = _currentOwner.NetworkSettings.TeamIndex;
            RpcSetTransferredMinionLayer(tempMinion.gameObject, ownerTeamIndex);
            RpcAddMinion(tempMinion.gameObject);
        }
    }

    private void SpawnNewLead()
    {
        var spawnPoint = new Vector3(UnityEngine.Random.Range(0, _randomSpawnDistance),
            UnityEngine.Random.Range(0, _randomSpawnDistance), 
            UnityEngine.Random.Range(0, _randomSpawnDistance));
        _minionLead = Instantiate(_minionLeadPref, transform.position + spawnPoint, Quaternion.identity);
        NetworkServer.Spawn(_minionLead.gameObject);
        _minionLead.MyCamp = this;
        RpcAddMinionLead(_minionLead.gameObject);
    }

    private void FullRespawnCamp()
    {
        List<MinionComponent> controlledMinions = new List<MinionComponent>(_minions);
        foreach (var minion in controlledMinions)
        {
            if (minion != null && minion.netIdentity != null && minion.netIdentity.connectionToClient != null)
            {
                _minions.Remove(minion);
                minion.MyCamp = null;
                RpcRemoveMinion(minion.gameObject);
            }
        }

        if (_minionLead == null)
        {
            SpawnNewLead();
        }

        _minions.Clear();
        for (int i = 0; i < _initialMinionCount && i < _minionPrefs.Count; i++)
        {
            var spawnPoint = new Vector3(UnityEngine.Random.Range(0, _randomSpawnDistance),
                UnityEngine.Random.Range(0, _randomSpawnDistance),
                UnityEngine.Random.Range(0, _randomSpawnDistance));
            var tempMinion = Instantiate(_minionPrefs[i], transform.position + spawnPoint, Quaternion.identity);
            _minions.Add(tempMinion);
            tempMinion.MyCamp = this;
            NetworkServer.Spawn(tempMinion.gameObject);
            RpcAddMinion(tempMinion.gameObject);
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

            if (_campStatus == CampStatus.Neutral)
            {
                Spawn();
            }
            else if (!_isLeadTaken && _campStatus != CampStatus.Neutral)
            {
                SpawnControlledMinions();
            }
        }
    }

    private IEnumerator CheckSurrenderJob()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(_spawnDelayMinions + 1);

            if (_campStatus != CampStatus.Neutral) continue;

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

                if (_campStatus != CampStatus.Neutral) break;
            }

            if (_campStatus != CampStatus.Neutral) continue;

            foreach (var hero in _attackers)
            {
                if (hero == null) continue;

                TargetShowSurrenderUI(hero.netIdentity.connectionToClient);
            }
            
            foreach (var timers in _attackerTimers.Values)
            {
                StopCoroutine(timers);
            }

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
       
        _campStatus = teamIndex == 1 ? CampStatus.DarkTeam : CampStatus.LightTeam;
        _isLeadTaken = isTakeLead;
        _currentOwner = clickedHero;
        
        SetCampStatus(_campStatus);
       
        if (isTakeLead)
        {
            if (_minionLead == null || _minionLead.Health.CurrentValue <= 0)
            {
                SpawnNewLead();
            }
            TransferLeadToHero(clickedHero);
        }
        else
        {
            TransferMinionsToHero(clickedHero);
        }
       
        _attackerTimers.Clear();
        _attackers.Clear();
       
        RpcHideSurrenderUI();
        UpdateMinionsLayersForAllPlayers();
    }

    [ClientRpc]
    private void SetCampStatus(CampStatus status)
    {
        _campStatus = status;
        UpdateCampColor();
    }
    
    private void UpdateCampColor()
    {
        if (_campColor == null) return;

        Color color;
        switch (_campStatus)
        {
            case CampStatus.Neutral:
                color = Color.gray;
                break;
            case CampStatus.DarkTeam:
                color = Color.red;
                break;
            case CampStatus.LightTeam:
                color = Color.blue;
                break;
            default:
                color = Color.gray;
                break;
        }
        _campColor.material.color = color;
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
        if (!_minions.Contains(tempMinion))
        {
            _minions.Add(tempMinion);
        }
        tempMinion.MyCamp = this;
       
        UpdateMinionLayersForLocalPlayer(minion);
    }

    [ClientRpc]
    private void RpcAddMinionLead(GameObject minion)
    {
        var tempMinion = minion.GetComponent<MinionComponent>();
        _minionLead = tempMinion;

        UpdateMinionLayersForLocalPlayer(minion);
    }
    
    [ClientRpc]
    private void RpcRemoveMinion(GameObject minion)
    {
        if (minion == null) return;
        
        var tempMinion = minion.GetComponent<MinionComponent>();
        if (tempMinion != null)
        {
            _minions.Remove(tempMinion);
            tempMinion.MyCamp = null;
        }
    }

    [ClientRpc]
    private void RpcRemoveMinionLeadAndSetLayer(GameObject leadMinion, int ownerTeamIndex)
    {
        if (_minionLead != null)
        {
            _minionLead.MyCamp = null;
            _minionLead = null;
        }

        if (leadMinion == null) return;

        foreach (var player in _players)
        {
            if (player == null) continue;

            var hero = player.GetComponent<HeroComponent>();
            if (hero == null || !hero.isOwned) continue;

            if (hero.isOwned)
            {
                int localTeamIndex = hero.NetworkSettings.TeamIndex;
                string layerName = (localTeamIndex == ownerTeamIndex) ? "Allies" : "Enemy";
                leadMinion.layer = LayerMask.NameToLayer(layerName);
                return;
            }
        }
    }
    
    [ClientRpc]
    private void RpcSetTransferredMinionLayer(GameObject minion, int ownerTeamIndex)
    {
        if (minion == null) return;

        foreach (var player in _players)
        {
            if (player == null) continue;

            var hero = player.GetComponent<HeroComponent>();
            if (hero == null || !hero.isOwned) continue;

            if (hero.isOwned)
            {
                int localTeamIndex = hero.NetworkSettings.TeamIndex;
                string layerName;

                if (localTeamIndex == ownerTeamIndex)
                {
                    layerName = "Allies";
                }
                else
                {
                    layerName = "Enemy";
                }

                minion.layer = LayerMask.NameToLayer(layerName);
                return;
            }
        }
    }
   
    private void SetMinionLayer(GameObject minion, int clientTeamIndex)
    {
        if (minion == null)
        {
            return;
        }

        string layerName;
       
        switch (_campStatus)
        {
            case CampStatus.Neutral:
                layerName = "Enemy";
                break;
               
            case CampStatus.LightTeam:
                layerName = (clientTeamIndex == 2) ? "Allies" : "Enemy";
                break;
               
            case CampStatus.DarkTeam:
                layerName = (clientTeamIndex == 1) ? "Allies" : "Enemy";
                break;
               
            default:
                layerName = "Enemy";
                break;
        }
       
        minion.layer = LayerMask.NameToLayer(layerName);
    }
   
    private void UpdateMinionLayersForLocalPlayer(GameObject minion)
    {
        if (minion == null)
        {
            return;
        }

        foreach (var player in _players)
        {
            if (player == null)
            {
                continue;
            }

            var hero = player.GetComponent<HeroComponent>();
            if (hero == null || !hero.isOwned)
            {
                continue;
            }

            if (hero.isOwned)
            {
                int localTeamIndex = hero.NetworkSettings.TeamIndex;
                SetMinionLayer(minion, localTeamIndex);
                return;
            }
        }
    }

    [ClientRpc]
    private void UpdateMinionsLayersForAllPlayers()
    {
        if (_minionLead != null)
        {
            UpdateMinionLayersForLocalPlayer(_minionLead.gameObject);
        }
       
        foreach (var minion in _minions)
        {
            UpdateMinionLayersForLocalPlayer(minion.gameObject);
        }
    }

    private void TransferLeadToHero(HeroComponent hero)
    {
        if (hero == null || hero.netIdentity == null || hero.netIdentity.connectionToClient == null)
        {
            return;
        }

        if (_minionLead != null)
        {
            int ownerTeamIndex = hero.NetworkSettings.TeamIndex;
            
            _minionLead.SetAuthority(hero.netIdentity.connectionToClient);
            hero.SpawnComponent.AddUnit(_minionLead);

            var leadToRemove = _minionLead;

            _minionLead = null;
            leadToRemove.MyCamp = null;
            
            RpcRemoveMinionLeadAndSetLayer(leadToRemove.gameObject, ownerTeamIndex);
        }
    }

    private void TransferMinionsToHero(HeroComponent hero)
    {
        if (hero == null || hero.netIdentity == null || hero.netIdentity.connectionToClient == null)
        {
            return;
        }
        
        int ownerTeamIndex = hero.NetworkSettings.TeamIndex;
        
        foreach (var minion in _minions)
        {
            if (minion != null)
            {
                RpcSetTransferredMinionLayer(minion.gameObject, ownerTeamIndex);
                
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
