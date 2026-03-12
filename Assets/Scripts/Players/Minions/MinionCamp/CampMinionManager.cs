using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class CampMinionManager : NetworkBehaviour
{
    private MinionComponent _minionLead;
    private List<MinionComponent> _minions = new();
    private readonly SyncList<GameObject> _playersSyncList = new SyncList<GameObject>();
    private List<Character> _players = new();
    private MinionCamp _camp;

    public void Initialize(MinionCamp camp)
    {
        _camp = camp;
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        _playersSyncList.OnChange += OnPlayersListChanged;

        foreach (var go in _playersSyncList)
        {
            var character = go.GetComponent<Character>();
            if (character != null && !_players.Contains(character))
            {
                _players.Add(character);
            }
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        _playersSyncList.OnChange -= OnPlayersListChanged;
    }

    private void OnPlayersListChanged(SyncList<GameObject>.Operation op, int index, GameObject item)
    {
        switch (op)
        {
            case SyncList<GameObject>.Operation.OP_ADD:
                var character = item.GetComponent<Character>();
                if (character != null && !_players.Contains(character))
                {
                    _players.Add(character);
                }
                break;
            case SyncList<GameObject>.Operation.OP_REMOVEAT:
                if (index < _players.Count)
                {
                    _players.RemoveAt(index);
                }
                break;
        }
    }

    public void AddPlayer(Character player)
    {
        if (player != null && !_playersSyncList.Contains(player.gameObject))
        {
            _playersSyncList.Add(player.gameObject);
            _players.Add(player);
        }
    }

    public MinionComponent GetLead() => _minionLead;
    public List<MinionComponent> GetMinions() => _minions;

    public void SetLead(MinionComponent lead)
    {
        _minionLead = lead;
        if (lead != null)
        {
            lead.MyCamp = _camp;
            RpcSetLead(lead.gameObject);
        }
    }

    public void AddMinion(MinionComponent minion)
    {
        if (minion != null && !_minions.Contains(minion))
        {
            _minions.Add(minion);
            minion.MyCamp = _camp;
            RpcAddMinion(minion.gameObject);
        }
    }

    public void OnMinionDied(MinionComponent minion)
    {
        if (!isServer || minion == null) return;

        if (minion == _minionLead)
        {
            _minionLead = null;
        }
        else
        {
            _minions.Remove(minion);
        }
    }

    public void ClearAllMinions()
    {
        _minions.Clear();
    }

    public void ClearControlledMinions()
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
    }

    public float GetTotalCurrentHP(Vector3 campPosition, float maxDistance)
    {
        float totalHP = 0;

        if (_minionLead == null && _minions.Count == 0)
            return 0;

        if (_minionLead != null && Vector3.Distance(campPosition, _minionLead.transform.position) <= maxDistance)
        {
            totalHP = _minionLead.Health.CurrentValue;
        }

        foreach (var minion in _minions)
        {
            if (minion != null)
            {
                totalHP += minion.Health.CurrentValue;
            }
        }

        return totalHP;
    }

    public float GetTotalMaxHP()
    {
        float totalMaxHP = 0;

        if (_minionLead != null)
        {
            totalMaxHP = _minionLead.Health.MaxValue;
        }

        foreach (var minion in _minions)
        {
            if (minion != null)
            {
                totalMaxHP += minion.Health.MaxValue;
            }
        }

        return totalMaxHP;
    }

    public void TransferLeadToHero(Character hero)
    {
        if (hero == null || hero.netIdentity == null || hero.netIdentity.connectionToClient == null)
            return;

        if (_minionLead != null)
        {
            int ownerTeamIndex = hero.NetworkSettings.TeamIndex;

            _minionLead.SetAuthority(hero.netIdentity.connectionToClient);
            hero.SpawnComponent.AddUnit(_minionLead);

            var leadToRemove = _minionLead;
            _minionLead = null;
            leadToRemove.MyCamp = null;

            RpcRemoveLeadAndSetLayer(leadToRemove.gameObject, ownerTeamIndex);
        }
    }

    public void TransferMinionsToHero(Character hero)
    {
        if (hero == null || hero.netIdentity == null || hero.netIdentity.connectionToClient == null)
            return;

        int ownerTeamIndex = hero.NetworkSettings.TeamIndex;

        foreach (var minion in _minions)
        {
            if (minion != null)
            {
                SetMinionLayerForOwner(minion.gameObject, ownerTeamIndex);
                minion.SetAuthority(hero.netIdentity.connectionToClient);
                hero.SpawnComponent.AddUnit(minion);
            }
        }
    }

    public void UpdateAllMinionLayers()
    {
        RpcUpdateAllLayers();
    }

    [ClientRpc]
    private void RpcUpdateAllLayers()
    {
        if (_minionLead != null)
        {
            UpdateMinionLayerForLocalPlayer(_minionLead.gameObject);
        }

        foreach (var minion in _minions)
        {
            if (minion != null)
            {
                UpdateMinionLayerForLocalPlayer(minion.gameObject);
            }
        }
    }

    [ClientRpc]
    public void SetMinionLayerForOwner(GameObject minion, int ownerTeamIndex)
    {
        if (minion == null) return;

        foreach (var player in _players)
        {
            if (player == null) continue;

            var hero = player.GetComponent<Character>();
            if (hero == null || !hero.isOwned) continue;

            int localTeamIndex = hero.NetworkSettings.TeamIndex;
            string layerName = (localTeamIndex == ownerTeamIndex) ? "Allies" : "Enemy";
            minion.layer = LayerMask.NameToLayer(layerName);
            return;
        }
    }

    private void UpdateMinionLayerForLocalPlayer(GameObject minion)
    {
        if (minion == null) return;

        foreach (var player in _players)
        {
            if (player == null) continue;

            var hero = player.GetComponent<Character>();

            if (hero == null || !hero.isOwned) continue;

            int localTeamIndex = hero.NetworkSettings.TeamIndex;
            SetMinionLayer(minion, localTeamIndex);
            return;
        }
    }

    private void SetMinionLayer(GameObject minion, int clientTeamIndex)
    {
        if (minion == null) return;

        CampStatus status = _camp._campStatus;
        string layerName;

        switch (status)
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

    [ClientRpc]
    private void RpcSetLead(GameObject lead)
    {
        if (lead == null) return;

        var tempMinion = lead.GetComponent<MinionComponent>();
        _minionLead = tempMinion;

        if (tempMinion != null)
        {
            tempMinion.MyCamp = _camp;
        }

        UpdateMinionLayerForLocalPlayer(lead);
    }

    [ClientRpc]
    private void RpcAddMinion(GameObject minion)
    {
        if (minion == null) return;

        var tempMinion = minion.GetComponent<MinionComponent>();
        if (tempMinion != null && !_minions.Contains(tempMinion))
        {
            _minions.Add(tempMinion);
            tempMinion.MyCamp = _camp;
        }

        UpdateMinionLayerForLocalPlayer(minion);
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
    private void RpcRemoveLeadAndSetLayer(GameObject leadMinion, int ownerTeamIndex)
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

            var hero = player.GetComponent<Character>();
            if (hero == null || !hero.isOwned) continue;

            int localTeamIndex = hero.NetworkSettings.TeamIndex;
            string layerName = (localTeamIndex == ownerTeamIndex) ? "Allies" : "Enemy";
            leadMinion.layer = LayerMask.NameToLayer(layerName);
            return;
        }
    }
}
