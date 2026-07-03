using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class HellTeleportSkill : Skill
{
    [SerializeField] private GameObject _hellZonePrefab;
    [SerializeField] private float _duration            = 4f;

    private Dictionary<Skill,AttributeModifier> _affectedSkills = new Dictionary<Skill, AttributeModifier>();
    private float _accumulatedFireDamage = 0f;
    private float _fireDamageThreshold   = 100f;
    private float _regenSlowMultiplier = 2f;

    private Vector3   _heroSavedPos;
    private Vector3   _targetSavedPos;
    private Character _hellTarget;
    private GameObject _spawnedHell;
    private Coroutine  _hellRoutine;

    protected override int  AnimTriggerCastDelay => 0;
    protected override int  AnimTriggerCast      => 0;

    protected override bool IsCanCast =>
        Targeting.GetTarget() != null &&
        Vector3.Distance(Targeting.GetTarget().Transform.position, transform.position) <= AreaInfo.Radius;

    #region ChargeCounter
    private void TrackFireDamage(Damage damage, GameObject obj)
    {
        if (damage.School != Schools.Fire) return;

        _accumulatedFireDamage += damage.Value;

        while (_accumulatedFireDamage >= _fireDamageThreshold)
        {
            _accumulatedFireDamage -= _fireDamageThreshold;
            TargetAddCharge(_hero.gameObject);

            if (Chargers > 0)
            {
                Disactive = false;
            }
        }
    }
    
    [TargetRpc]
    private void TargetAddCharge(GameObject obj)
    {
        if (_currentChargers < _maxCharges)
        {
            Chargers = _currentChargers + 1;
        }

        CheckChargers();
    }

    private void CheckChargers()
    {
        if (_currentChargers > 0)
        {
            Disactive = false;
        }
        else
        {
            Disactive = true;
        }

        Charges.SendCurrentChange(_currentChargers);
    }

    protected override void UseCooldownOrCharges()
    {
        if (_currentChargers <= 0) return;
        Chargers = _currentChargers - 1;
        CheckChargers();
    }
    

    #endregion
    
    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);
        hero.DamageTracker.OnDamageTracked += TrackFireDamage;
    }

    private void OnDisable()
    {
        if (Hero == null) return;
        Hero.DamageTracker.OnDamageTracked -= TrackFireDamage;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (Targeting.GetTempTarget()?.Character == null)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget(Targeting.GetMousePoint(), 0.5f, canTargetSelf: false);
                if (Targeting.GetTempTarget()?.Character == Hero)
                    Targeting.ClearTempTarget();
            }
            yield return null;
        }

        Targeting.SetTarget(Targeting.GetTempTarget()?.Character);
        var info = new TargetInfo();
        info.AddTarget(Targeting.GetTarget()?.Character);
        callbackDataSaved(info);
    }

    protected override IEnumerator CastJob()
    {
        var target = Targeting.GetTarget()?.Character;
        if (target == null) yield break;
        
        if (_hero.Resources.TryGetValue(ResourceType.Energy, out var energy))
            energy.CmdAddRegenModifier(Cost.BaseCost, _regenSlowMultiplier, isFast: false);

        CmdBeginHell(target.gameObject);
        yield return null;
    }

    [Command]
    private void CmdBeginHell(GameObject targetGO)
    {
        var target = targetGO?.GetComponent<Character>();
        if (target == null) return;

        if (_spawnedHell == null)
        {

            var hellObj = Instantiate(_hellZonePrefab);
            _spawnedHell = hellObj;
            NetworkServer.Spawn(hellObj);
        }

        TargetSetFireSkillFree(_hero.gameObject, true);
        SetFireSkillFree(_hero.gameObject, true);

        var hell          = _spawnedHell.GetComponent<HellZone>();
        Vector3 heroPos = hell.HeroSpawn;
        Vector3 targetPos = hell.TargetSpawn;

        _heroSavedPos   = _hero.transform.position;
        _targetSavedPos = target.transform.position;
        _hellTarget     = target;

        _hero.Move.TargetRpcSetTransformPosition(heroPos);
        target.Move.TargetRpcSetTransformPosition(targetPos);

        TargetRpcUpdateCamera(connectionToClient, heroPos, _spawnedHell);
        if (target.connectionToClient != null)
            TargetRpcUpdateCamera(target.connectionToClient, targetPos, _spawnedHell);

        if (_hellRoutine != null) StopCoroutine(_hellRoutine);
        _hellRoutine = StartCoroutine(HellReturnRoutine(target));
    }

    [TargetRpc]
    private void TargetRpcUpdateCamera(NetworkConnectionToClient conn,
        Vector3 playerNewPos, GameObject hellZoneGO)
    {
        var hell = hellZoneGO?.GetComponent<HellZone>();
        if (hell == null) return;

        var limiter = FindObjectOfType<CameraBoxLimiter>();
        if (limiter != null && hell.CameraBounds != null)
            limiter.SetTemporaryBounds(hell.CameraBounds);

        if (Camera.main != null)
        {
            var cam = Camera.main.GetComponent<CameraMoveNew>();
            Camera.main.transform.position = cam != null
                ? playerNewPos + cam.offset
                : playerNewPos + new Vector3(0f, 15f, -15f);
        }
    }
    private IEnumerator HellReturnRoutine(Character target)
    {
        yield return new WaitForSeconds(_duration);

        _hero.Move.TargetRpcSetTransformPosition(_heroSavedPos);
        target.Move.TargetRpcSetTransformPosition(_targetSavedPos);
        
        TargetRpcRestoreCamera(connectionToClient, _heroSavedPos);
        if (target.connectionToClient != null)
            TargetRpcRestoreCamera(target.connectionToClient, _targetSavedPos);

        TargetSetFireSkillFree(_hero.gameObject, false);
        SetFireSkillFree(_hero.gameObject, false);
        
        _hellRoutine = null;
    }
    
    private void SetFireSkillFree(GameObject obj, bool value)
    {

    }
    
    [TargetRpc]
    private void TargetSetFireSkillFree(GameObject obj, bool value)
    {
        foreach (var skill in _hero.Abilities.Abilities)
        {
            if (skill.Info.School == Schools.Fire)
            {
                if (value)
                {
                    AttributeModifier newModifier = new AttributeModifier(-skill.Cost.BaseCost, ModifierType.Flat);
                    skill.Attributes[SkillAttributeName.ResourceCost].AddModifier(newModifier);
                    _affectedSkills.Add(skill,newModifier);
                }
                else
                {
                    if (_affectedSkills.TryGetValue(skill, out var affectedSkill))
                    {
                        skill.Attributes[SkillAttributeName.ResourceCost].RemoveModifier(affectedSkill);
                        _affectedSkills.Remove(skill);
                    }
                }
            }
        }
    }

    [TargetRpc]
    private void TargetRpcRestoreCamera(NetworkConnectionToClient conn, Vector3 returnPos)
    {
        FindObjectOfType<CameraBoxLimiter>()?.RestoreOriginalBounds();

        if (Camera.main != null)
        {
            var cam = Camera.main.GetComponent<CameraMoveNew>();
            Camera.main.transform.position = cam != null
                ? returnPos + cam.offset
                : returnPos + new Vector3(0f, 15f, -15f);
        }
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
        Targeting.ClearTempTarget();
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0)
            Targeting.SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
    }
}