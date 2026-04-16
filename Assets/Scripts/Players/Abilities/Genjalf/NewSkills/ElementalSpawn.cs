using System;
using System.Collections;
using System.Collections.Generic;
using Gangdollarff.EarthElemental;
using Mirror;
using UnityEngine;

public class ElementalSpawn : Skill
{
    private MinionComponent _currentElemental;
    private Vector3 _position;
    private Elementals _selectedElemental = Elementals.None;
    private Elementals _previousElemental;
    public Elementals SelectedElemental
    {
        get => _selectedElemental;
        set => _selectedElemental = value;
    }

    #region Elementals Talents

    private bool _elementalsAuraTalent;
    private bool _elementalsShieldsTalent;
    #endregion

    protected override bool IsCanCast => Vector3.Distance(_position, transform.position) <= AreaInfo.Radius;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    private Action OnCurrentElemantalDestroy;

    #region Talent Enabling Methods

    public void IsElementalsAuraEnabled(bool value)
    {
        if (_elementalsAuraTalent != value)
        {
            _elementalsAuraTalent = value;
            if(_currentElemental)
                TryActivateMinionTalent(_selectedElemental,true);
        }
    }

    public void IsElementalsShieldsEnabled(bool value)
    {
        if (_elementalsShieldsTalent != value)
        {
            _elementalsShieldsTalent = value;
            if(_currentElemental)
                TryActivateMinionTalent(_selectedElemental,true);
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
            TryActivateMinionTalent(_previousElemental,false);
            _currentElemental = null;
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
        _currentElemental = elementalGO.GetComponent<MinionComponent>();
        _currentElemental.CharacterParent = _hero;
        TryActivateMinionTalent(type,true);
    }

    protected override void ClearData()
    {
        _position = Vector2.zero;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        _previousElemental = _selectedElemental;
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

    private void TryActivateMinionTalent(Elementals elementalType, bool enable)
    {
        if (_currentElemental == null) 
            return;

        switch (elementalType)
        {
            case Elementals.Air:
                HandleAura<AirElement>(enable);
                HandleSkill<ColdShield>(enable);
                break;

            case Elementals.Earth:
                HandleAura<EarthElementalAuras>(enable);
                HandleSkill<EarthPetrificationSkill>(enable);
                break;

            case Elementals.Fire:
                HandleAura<HotBloodAura>(enable);
                HandleSkill<FireShield>(enable);
                break;

            case Elementals.Water:
                HandleWaterTalents(enable);
                break;
        }
    }

    #region Активация талантов элементалей
    private void HandleAura<T>(bool shouldBeActive) where T : AuraStateHandler
    {
        var auraComponent = _currentElemental.GetComponent<T>();
        if (auraComponent == null)
            return;

        bool actuallyActivate = _elementalsAuraTalent && shouldBeActive;

        auraComponent.ActivateAura(actuallyActivate);
    }

    private void HandleSkill<T>(bool shouldBeActive) where T : Skill
    {
        var skillComponent = _currentElemental.GetComponent<T>();
        if (skillComponent == null)
            return;

        bool actuallyActivate = _elementalsShieldsTalent && shouldBeActive;

        if (actuallyActivate)
            _currentElemental.Abilities.ActivateSkill(skillComponent);
        else
            _currentElemental.Abilities.DeactivateSkill(skillComponent);
    }

    private void HandleWaterTalents(bool shouldBeActive)
    {
        var magicWater = _currentElemental.GetComponent<MagicWaterPassive>();
        if (magicWater == null)
            return;

        bool actuallyActivate = _elementalsAuraTalent && shouldBeActive;

        if (actuallyActivate)
            _currentElemental.Abilities.ActivateSkill(magicWater);
        else
            _currentElemental.Abilities.DeactivateSkill(magicWater);

        magicWater.EnableMagicWaterAura(actuallyActivate);
    }
    #endregion

    [Command]
    private void CmdDestroyUnit(int index)
    {
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
