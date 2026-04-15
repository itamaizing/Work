using System;
using System.Collections;
using System.Collections.Generic;
using Gangdollarff.EarthElemental;
using Mirror;
using UnityEngine;

public class ElementalSpawn : Skill
{
    private Character _currentElemental;
    private Vector3 _position;
    private Elementals _selectedElemental = Elementals.None;
    public Elementals SelectedElemental
    {
        get => _selectedElemental;
        set => _selectedElemental = value;
    }

    #region Elementals Talents

    private bool _isHotAuraTalent;
    #endregion

    protected override bool IsCanCast => Vector3.Distance(_position, transform.position) <= AreaInfo.Radius;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    #region Talent Enabling Methods

    public void IsHotAuraEnabled(bool value)
    {
        if (_isHotAuraTalent != value)
        {
            _isHotAuraTalent = value;
            if(_currentElemental)
                ConfigureSpawnedElemental(_selectedElemental);
        }
    }

    #endregion

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _position = targetInfo.Points[0];
    }

    protected override IEnumerator CastJob()
    {
        while (_selectedElemental == Elementals.None)
        {
            yield return null;
        }

        if (Hero.SpawnComponent.Units.Count > 0)
        {
            CmdDestroyUnit(0);
            Hero.SpawnComponent.Units.RemoveAt(0);
        }
        
        CmdSpawnElemental(_position, _selectedElemental);

        yield return null;
    }
    
    [Command]
    private void CmdSpawnElemental(Vector3 position, Elementals type)
    {
        int index = (int)type;
        var spawned = Hero.SpawnComponent.SpawnUnit(index,position);

        if (spawned != null)
            TargetRpcOnElementalSpawned(connectionToClient, spawned.gameObject, type);
    }

    [TargetRpc]
    private void TargetRpcOnElementalSpawned(NetworkConnectionToClient conn, GameObject elementalGO, Elementals type)
    {
        if (elementalGO == null) return;
        _currentElemental = elementalGO.GetComponent<Character>();
        ConfigureSpawnedElemental(type);
    }

    protected override void ClearData()
    {
        _position = Vector2.zero;
        _currentElemental = null;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        _selectedElemental = Elementals.None;
        while (_position == Vector3.zero)
        {
            if (GetMouseButton)
            {
                _position = Targeting.GetMousePoint();
            }
            yield return null;
        }
        TargetInfo targetInfo = new();
        targetInfo.Points.Add(_position);
        callbackDataSaved(targetInfo);
    }

    private void ConfigureSpawnedElemental(Elementals elementalType)
    {
        if (_currentElemental)
        {
            switch (elementalType)
            {
                case Elementals.Air:
                    break;
                case Elementals.Earth:
                    _currentElemental.GetComponent<EarthElementalAuras>().SetActive(_isHotAuraTalent);
                    break;
                case Elementals.Fire:
                    _currentElemental.GetComponent<HotBloodAura>().SetActive(_isHotAuraTalent);
                    break;
                case Elementals.Water:
                    break;
            }
        }
    }

    [Command]
    private void CmdDestroyUnit(int index)
    {
        Debug.Log(Hero.SpawnComponent.Units[index].gameObject.name);
        NetworkServer.Destroy(Hero.SpawnComponent.Units[index].gameObject);

        Hero.SpawnComponent.Units.RemoveAt(index);
    }
}

//Базируется на порядке префабов в SpawnComponent
public enum Elementals
{
    None = -1,
    Air = 0,
    Earth = 1,
    Fire = 2,
    Water = 3,
}
