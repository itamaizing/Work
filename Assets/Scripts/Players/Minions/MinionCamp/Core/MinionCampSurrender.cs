using System.Collections.Generic;
using Mirror;
using UnityEngine;

public partial class MinionCamp
{

    partial void TickSurrender(float dt)
    {
        if (_offerActive && Time.time >= _offerDeadline)
            ResolveOffer(null, SurrenderChoice.Kill);

        var slots = _pop.Slots;
        for (int i = slots.Count - 1; i >= 0; i--)
        {
            var slot = slots[i];
            if (!slot.IsPendingRespawn) continue;

            slot.RespawnTimer -= dt;
            if (slot.RespawnTimer > 0f) continue;

            RespawnControlledCreep(slot);
        }
    }

    private void RespawnControlledCreep(CreepSlot slot)
    {
        slot.RespawnTimer = -1f;
        UntrackCreepRespawn(slot.Id);

        var unit = SpawnUnit(slot.Data, _level);
        if (unit == null)
        {
            _pop.RemoveSlot(slot);
            return;
        }

        slot.Instance = unit;
        slot.Level = _level;

        if (_owner != null) GiveToHero(unit, _owner);
        else unit.SetOwnerTeam(_ownerTeam);
    }

    partial void EvaluateSurrender()
    {
        if (_offerActive || _offerConsumed) return;
        if (_ownerTeam != CampTeam.Neutral || _phase != CampPhase.Active) return;
        if (_pop.Leader == null || _maxHp <= 0f || _attackers.Count == 0) return;

        if (_pop.TotalCurrentHp() / _maxHp >= _surrenderThreshold) return;

        BeginOffer();
    }

    private void BeginOffer()
    {
        Debug.Log("[Minion Camp] Порог сдачи пройден", this);

        _offerActive = true;
        _offerConsumed = true;
        _offerDeadline = Time.time + _surrenderOfferTimeout;

        _attackers.Freeze(true);
        SetAllInvulnerable(true);

        _attackers.CopyTo(_heroBuffer);
        foreach (var hero in _heroBuffer)
        {
            var conn = hero.netIdentity != null ? hero.netIdentity.connectionToClient : null;
            if (conn != null) TargetShowSurrenderUI(conn);
        }
    }

    partial void ResolveOffer(Character hero, SurrenderChoice choice)
    {
        _offerActive = false;
        _attackers.Freeze(false);
        SetAllInvulnerable(false);

        if (hero != null)
        {
            switch (choice)
            {
                case SurrenderChoice.TakeCreeps: ApplyTake(hero); break;
                case SurrenderChoice.KeepLeader: ApplyKeep(hero); break;
            }
        }

        _attackers.Clear();
        RpcHideSurrenderUI();
    }

    private void CancelOffer()
    {
        if (!_offerActive) return;

        _offerActive = false;
        _attackers.Freeze(false);
        SetAllInvulnerable(false);
        RpcHideSurrenderUI();
    }

    private void SetAllInvulnerable(bool value)
    {
        _pop.CollectAlive(_unitBuffer);
        foreach (var unit in _unitBuffer) unit.SetInvulnerable(value);
    }

    private void ApplyTake(Character hero)
    {
        int team = hero.NetworkSettings.TeamIndex;

        DismissTakenGroup(team);

        var group = new List<MinionComponent>();
        _pop.CollectAlive(_unitBuffer);
        foreach (var unit in _unitBuffer)
        {
            Detach(unit);
            GiveToHero(unit, hero);
            group.Add(unit);
        }

        _pop.Clear();
        _takenGroups[team] = group;

        EnterWaitingRespawn(CampClearReason.LeaderTaken);
    }

    private void ApplyKeep(Character hero)
    {
        int team = hero.NetworkSettings.TeamIndex;
        _owner = hero;
        _ownerTeam = team;

        var leader = _pop.Leader;
        if (leader != null)
        {
            leader.SetOwnerTeam(team);
            leader.ClearNegativeEffectsFrom(team);
        }

        foreach (var slot in _pop.Slots)
        {
            slot.Controlled = true;
            if (slot.IsAlive) GiveToHero(slot.Instance, hero);
        }
    }

    private void GiveToHero(MinionComponent unit, Character hero)
    {
        var conn = hero.netIdentity != null ? hero.netIdentity.connectionToClient : null;
        if (conn != null)
        {
            unit.SetAuthority(conn);
            hero.SpawnComponent.AddUnit(unit);
        }

        int team = hero.NetworkSettings.TeamIndex;
        unit.SetInvulnerable(false);
        unit.SetOwnerTeam(team);
        unit.ClearNegativeEffectsFrom(team);
    }

    private void Detach(MinionComponent unit)
    {
        unit.Destroyed -= OnUnitDestroyed;
        unit.Camp = null;
    }

    private void DismissTakenGroup(int team)
    {
        if (!_takenGroups.TryGetValue(team, out var group)) return;

        foreach (var unit in group)
        {
            if (unit != null && !unit.IsDead)
                NetworkServer.Destroy(unit.gameObject);
        }

        _takenGroups.Remove(team);
    }

    partial void OnLeaderLost()
    {
        CancelOffer();

        if (_ownerTeam != CampTeam.Neutral)
            Neutralize();
    }

    private void Neutralize()
    {
        foreach (var slot in _pop.Slots)
            if (slot.IsAlive) Detach(slot.Instance);

        _pop.Clear();
        _creepRespawns.Clear();
        _owner = null;
        _ownerTeam = CampTeam.Neutral;
    }
    
    [TargetRpc]
    private void TargetShowSurrenderUI(NetworkConnectionToClient conn)
    {
        if (_surrenderUI == null) return;
        _surrenderUI.Setup(this);
        _surrenderUI.Show();
    }

    [ClientRpc]
    private void RpcHideSurrenderUI()
    {
        if (_surrenderUI != null) _surrenderUI.Hide();
    }
}