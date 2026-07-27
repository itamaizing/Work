using System.Collections;
using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System;

public class WombSpawn : Skill
{
    [SerializeField] private Character _player;
    [SerializeField] private SpawnComponent _spawnComponent;
    [SerializeField] private SummoningSwarm _summoningSwarm;
    [SerializeField] private float _radiusTarget = 0.5f;

    private bool _isClickedOnGround = false;

    private Vector3 _spawnPoint = Vector3.positiveInfinity;
    private readonly List<GameObject> _spawnedWombs = new();

    #region Talent
    private bool _isWombSpreadsMucus = false;
    private bool _isWombSpreadParasites = false;
    private bool _isSpawnGetomir;
    private bool _isSpawnSpikeMucus = false;

    public event Action<bool> OnSpawnGetomirChanged;
    public event Action<bool> OnWombSpreadsMucusChanged;
    public event Action<bool> OnWombSpreadsParasitesChanged;
    //public event Action<bool> OnSpawnSpikeMucus;

    public bool IsSpawnGetomir
    {
        get => _isSpawnGetomir;
        set
        {
            if (_isSpawnGetomir == value) return;

            _isSpawnGetomir = value;
            OnSpawnGetomirChanged?.Invoke(_isSpawnGetomir);
        }
    }

    public bool IsWombSpreadsMucus
    {
        get => _isWombSpreadsMucus;
        set
        {
            if (_isWombSpreadsMucus == value) return;

            _isWombSpreadsMucus = value;
            OnWombSpreadsMucusChanged?.Invoke(_isWombSpreadsMucus);
        }
    }

    public bool IsWombSpreadsParasites
    {
        get => _isWombSpreadParasites;
        set
        {
            if (_isWombSpreadParasites == value) return;

            _isWombSpreadParasites = value;
            OnWombSpreadsParasitesChanged?.Invoke(_isWombSpreadParasites);
        }
    }

    public bool IsSpawnSpikeMucus
    {
        get => _isSpawnSpikeMucus;
        set
        {
            if (_isSpawnSpikeMucus == value) return;

            _isSpawnSpikeMucus = value;
            //OnSpawnSpikeMucus?.Invoke(_isSpawnSpikeMucus);
        }
    }

    public void SpawnGetomir(bool value) => IsSpawnGetomir = value;
    public void WombSpreadsMucus(bool value) => IsWombSpreadsMucus = value;
    public void WombSpreadsParasites(bool value) => IsWombSpreadsParasites = value;
    public void SpawnSpikeMucus(bool value) => IsSpawnSpikeMucus = value;

    #region Skills Creatures

    private bool _isEffectTentaclesCreatures = false;

    public event Action<bool> OnEffectTentaclesCreatures;

    public bool IsEffectTentaclesCreatures
    {
        get => _isEffectTentaclesCreatures;
        set
        {
            if (_isEffectTentaclesCreatures == value) return;

            _isEffectTentaclesCreatures = value;
            OnEffectTentaclesCreatures?.Invoke(_isEffectTentaclesCreatures);
        }
    }

    public void EffectTentaclesCreatures(bool value) => IsEffectTentaclesCreatures = value;

    #endregion

    #endregion

    private LayerMask _alliesMask;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("Spell");
    protected override bool IsCanCast =>
    _summoningSwarm != null && _spawnPoint != Vector3.positiveInfinity && IsCanRadius();

    private bool IsCanRadius()
    {
        if (!IsValidVector(_spawnPoint)) return false;

        float distance = Vector3.Distance(Hero.transform.position, _spawnPoint);
        return distance <= AreaInfo.Radius;
    }

    private bool IsValidVector(Vector3 vector)
    {
        return !(float.IsNaN(vector.x) || float.IsNaN(vector.y) || float.IsNaN(vector.z) ||
                 float.IsInfinity(vector.x) || float.IsInfinity(vector.y) || float.IsInfinity(vector.z));
    }

    private void OnDisable()
    {
        OnSkillCanceled -= HandleSkillCanceled;
    }
    private void OnEnable()
    {
        OnSkillCanceled += HandleSkillCanceled;
    }

    private void Start()
    {
        _alliesMask = LayerMask.GetMask("Allies");
    }

    private void HandleSkillCanceled()
    {
        Targeting.ClearTarget();
        _skillRender.StopDrawRadius();
    }

    public void MoveStop()
    {
        Hero.Move.SetCanMove(false);
        if (Targeting.GetTarget()?.Character) _player.Move.LookAtPosition(Targeting.GetTarget().Character.transform.position);
        Hero.Move.StopMoveAndAnimationMove();
    }

    public void AnimTentaclesCast()
    {
        CommitUse();
        AnimStartCastCoroutine();
    }

    public void AnimTentaclesCastEnd()
    {
        AnimCastEnded();
    }

    protected override void ClearData()
    {
        _skillRender.IsOverrideClosestTarget = false;
        _isClickedOnGround = false;
        _skillRender.StopDrawRadius();

        _spawnPoint = Vector3.positiveInfinity;
        Targeting.ClearTarget();
        Hero.Move.SetCanMove(true);
        _player.Move.StopLookAt();
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        Vector3 targetPoint = Vector3.positiveInfinity;

        while (float.IsPositiveInfinity(targetPoint.x))
        {
            Vector3 mousePoint = Targeting.GetMousePoint();

            if (GetMouseButton) targetPoint = mousePoint;

            yield return null;
        }

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Points.Add(targetPoint);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (!IsValidVector(_spawnPoint)) yield break;

        bool hadCharges = _summoningSwarm != null && _summoningSwarm.ChargesSwarm > 0;

        if (hadCharges) _summoningSwarm.UseSwarmCharges(1);

        SpawnWomb(_spawnPoint);

        if (hadCharges) Cooldown.ForceEnd();

        ClearData();
        _skillRender.StopDrawRadius();
        yield return null;
    }


    public void SpawnWombExternal(Vector3 pos) => SpawnWomb(pos);
    
    private void SpawnWomb(Vector3 position)
    {
        if (!IsValidVector(position)) return;
        _spawnComponent.CmdSpawnEnemyPoint(position, Quaternion.identity, null, 0, false, Hero);
        CmdTentacleWomb();
    }

    [Command]
    private void CmdTentacleWomb()
    {
        RpcTentacleWomb();
        _skillRender.StopDrawRadius();
    }

    [ClientRpc]
    private void RpcTentacleWomb()
    {
        foreach (var womb in _spawnComponent.Units)
        {
            if (womb.TryGetComponent<CreatureSpawn>(out CreatureSpawn creatureSpawn)) creatureSpawn.WombSpawn = this;
            _spawnedWombs.Add(womb.gameObject);
        }
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _spawnPoint = targetInfo.Points[0];
        if (targetInfo.GetTargets().Count > 0) Targeting.SetTarget((Character)targetInfo.GetTargets()[0]);
    }
}