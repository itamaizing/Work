using UnityEngine.SceneManagement;
using Mirror;
using System.Collections;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class GrowTree : Skill
{
    [Header("GrowTree Settings")]
    [SerializeField] private Tree _treePrefab;
    [SerializeField] private MoveComponent moveComponent;
    [SerializeField] private float _moveDuration = 0.5f;
    [SerializeField] private List<Tree> _activeTrees;
    [SerializeField] private ObjectData treeData;

    [Header("Talents")]
    [SerializeField] private bool treeHealthTalent;
    [SerializeField] private bool treeMagicEvadeTalent;
    [SerializeField] private bool treeShotCooldownTalent;

    private Vector3 _targetPoint = Vector3.positiveInfinity;
    private Tree _currentTree;
    private ObjectHealth _healthTree;
    private float baseHealth;
    private float baseCastStreamDuration;
    private Coroutine _treeHealthCoroutine;
    private Coroutine _rangeWatch;
    private bool _isSpawnHero;

    private ShotsIntoSky _shotsIntoSky;
    private ShotIntoSky _shotIntoSky;

    protected override bool IsCanCast =>
        !float.IsPositiveInfinity(_targetPoint.x) &&
        IsPointInRadius(Radius, _targetPoint);

    protected override int AnimTriggerCastDelay => Animator.StringToHash("GrowTreeCastDelay");
    protected override int AnimTriggerCast => 0;

    private void Start()
    {
        SkillManager skillManager = Hero.Abilities;

        if (_shotsIntoSky == null) _shotsIntoSky = skillManager.Abilities.OfType<ShotsIntoSky>().FirstOrDefault();
        if (_shotIntoSky == null) _shotIntoSky = skillManager.Abilities.OfType<ShotIntoSky>().FirstOrDefault();

        baseHealth = treeData.MaxHealth;
        baseCastStreamDuration = CastStreamDuration;
    }

    private void OnEnable() => OnSkillCanceled += HandleSkillCanceled;
    private void OnDestroy() => OnSkillCanceled -= HandleSkillCanceled;

    private void StopRangeWatch()
    {
        if (_rangeWatch != null)
        {
            StopCoroutine(_rangeWatch);
            _rangeWatch = null;
        }
    }

    private void ResetData()
    {
        _isSpawnHero = false;
        _currentTree = null;
    }

    private IEnumerator CastDistanceWatcher()
    {
        const float checkInterval = 0.1f;

        try
        {
            while (true)
            {
                if (_currentTree == null)
                {
                    if (Vector3.Distance(_hero.transform.position, _targetPoint) > Radius)
                    {
                        TryCancel();
                        ResetData();
                        break;
                    }
                }
                else
                {
                    if (Vector3.Distance(_hero.transform.position, _currentTree.transform.position) > Radius)
                    {
                        TryCancel();
                        ResetData();
                        break;
                    }
                }

                yield return new WaitForSeconds(checkInterval);
            }
        }

        finally { _rangeWatch = null; }
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TreeHealthTalentEnter();

        _activeTrees.RemoveAll(t => t == null);
        CmdRemoveTree();

        int treeCount = _activeTrees.Count;
        CastStreamDuration = treeCount == 0 ? baseCastStreamDuration : baseCastStreamDuration * Mathf.Pow(2, treeCount);

        CmdCastStreamDurationWithTree();

        while (float.IsPositiveInfinity(_targetPoint.x) && !_disactive)
        {
            if (GetMouseButton)
            {
                var clickedCharacter = GetClickedCharacter(Hero);

                if (clickedCharacter != null && clickedCharacter == _hero)
                {
                    _targetPoint = _hero.transform.position;
                    _isSpawnHero = true;
                }

                else
                {
                    _targetPoint = GetMousePoint();
                    if (!IsPointInRadius(Radius, _targetPoint)) _targetPoint = Vector3.positiveInfinity;
                }
            }
            yield return null;
        }

        DrawDamageZone(_targetPoint);

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Points.Add(_targetPoint);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (_treePrefab == null) yield break;
        if (_rangeWatch != null)
        {
            StopCoroutine(_rangeWatch);
            _rangeWatch = null;
        }


        Vector3 spawnPos = _targetPoint;

        if (_rangeWatch == null) _rangeWatch = StartCoroutine(CastDistanceWatcher());

        _hero.Animator.SetTrigger(AnimTriggerCastDelay);
        _hero.NetworkAnimator.SetTrigger(AnimTriggerCastDelay);

        yield return new WaitForSeconds(CastStreamDuration / 3);

        StopDamageZone();

        if (_isSpawnHero) CmdSpawnTreeAndTeleport(_hero.transform.position);
        else CmdSpawnTree(spawnPos);

        yield return new WaitForSeconds(CastStreamDuration / 1.5f);

        _hero.Animator.ResetTrigger(Animator.StringToHash("GrowTreeCastDelay"));
        _hero.NetworkAnimator.ResetTrigger(Animator.StringToHash("GrowTreeCastDelay"));

        CmdCrossFade();
        _hero.Animator.CrossFade("GrowTreeCastDelayExit", 0.1f);

        ResetData();
        StopRangeWatch();   
    }

    #region Canceling a skill
    private void HandleSkillCanceled()
    {
        StopDamageZone();
        StopRangeWatch();

        if (_hero != null && _hero.Move != null) Hero.Animator.speed = 1;
        TreeHealthTalentExit();

        if (_currentTree != null) CmdRequestInterruptTree(_currentTree.netId);

        ResetData();
    }
    #endregion

    #region Auxiliary methods
    private Character GetClickedCharacter(Character hero)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            Vector3 clickPoint = hit.point;
            Collider[] hits = Physics.OverlapSphere(clickPoint, Area, TargetsLayers);
            if (hits.Length == 0) return null;
            foreach (Collider target in hits) if (target.TryGetComponent(out Character character) && character == hero) return character;
        }

        return null;
    }
    #endregion

    #region [Command] / Spawn
    [Command] private void CmdSetMaxHealth(float maxHealth) => treeData.MaxHealth = maxHealth;
    [Command] private void CmdRemoveTree() => _activeTrees.RemoveAll(tree => tree == null);

    [Command]
    private void CmdSpawnTree(Vector3 position)
    {
        var tree = Instantiate(_treePrefab, position, Quaternion.identity);
        _currentTree = tree;

        NetworkServer.Spawn(_currentTree.gameObject);
        SceneManager.MoveGameObjectToScene(_currentTree.gameObject, Hero.NetworkSettings.MyRoom);

        _healthTree = tree.GetComponent<ObjectHealth>();
        if (_healthTree != null)
        {
            float regenDuration = CastStreamDuration - CastStreamDuration / 3f;

            _healthTree.InitializeObject(treeData);
            if (treeData.MinEndurance) _healthTree.ServerStartFillHP(_healthTree.ObjectData.MaxHealth, regenDuration);

            if (treeMagicEvadeTalent) _healthTree.SetMagicEvade(100);

        }
        ResetShotCooldowns();

        _activeTrees.Add(tree);
        RpcClientAddTree(tree.GetComponent<NetworkIdentity>().netId, _currentTree);
    }

    [Command]
    private void CmdSpawnTreeAndTeleport(Vector3 position)
    {
        Debug.Log($"TargetPoint: {_targetPoint.x}");
        Vector3 spawnPosition = position + Vector3.down;

        var tree = Instantiate(_treePrefab, spawnPosition, Quaternion.identity);
        _currentTree = tree;
        NetworkServer.Spawn(_currentTree.gameObject);
        SceneManager.MoveGameObjectToScene(_currentTree.gameObject, Hero.NetworkSettings.MyRoom);

        RpcTeleportToTree(_currentTree.gameObject);

        _healthTree = _currentTree.GetComponent<ObjectHealth>();
        if (_healthTree != null)
        {
            _healthTree.InitializeObject(treeData);

            float regenDuration = CastStreamDuration - CastStreamDuration / 3f;

            if (treeData.MinEndurance) _healthTree.ServerStartFillHP(_healthTree.ObjectData.MaxHealth, regenDuration);

            if (treeMagicEvadeTalent) _healthTree.SetMagicEvade(100);
        }
        ResetShotCooldowns();

        _activeTrees.Add(tree);
        RpcClientAddTree(tree.GetComponent<NetworkIdentity>().netId, _currentTree);
    }

    [Command]
    private void CmdCastStreamDurationWithTree()
    {
        int treeCount = _activeTrees.Count;
        CastStreamDuration = treeCount == 0 ? baseCastStreamDuration : baseCastStreamDuration * Mathf.Pow(2, treeCount);
    }

    [Command]
    private void CmdRequestInterruptTree(uint treeNetId)
    {
        if (NetworkServer.spawned.TryGetValue(treeNetId, out NetworkIdentity networkIdentity) &&
            networkIdentity.TryGetComponent(out ObjectHealth objectHealth))
            objectHealth.ServerInterruptFillHP();
    }

    [Command]
    private void CmdCrossFade()
    {
        _hero.Animator.CrossFade("GrowTreeCastDelayExit", 0.1f);
    }

    [ClientRpc]
    private void RpcClientAddTree(uint netId, Tree currentTree)
    {
        _currentTree = currentTree;
        if (NetworkClient.spawned.TryGetValue(netId, out var networkIdentity)) _activeTrees.Add(networkIdentity.GetComponent<Tree>());
    }

    [ClientRpc]
    private void RpcTeleportToTree(GameObject tree)
    {
        if (tree != null)
        {
            Vector3 topOfTree = tree.transform.position + Vector3.up * 5f;
            moveComponent.TeleportToPositionSmooth(topOfTree, _moveDuration);
        }
    }

    [ClientRpc]
    private void ResetShotCooldowns()
    {
        if (!treeShotCooldownTalent) return;

        if (_shotsIntoSky != null && !_shotsIntoSky.IsCooldowned) _shotsIntoSky.ForceCooldownEnd();
        if (_shotIntoSky != null && !_shotIntoSky.IsCooldowned) _shotsIntoSky.ForceCooldownEnd();
    }
    #endregion

    #region Shot Tree Cooldown Talent
    public void ShotTreeCooldownTalent(bool value) => treeShotCooldownTalent = value;
    #endregion

    #region Talent for doubling HP
    public void treeHealthTalentActive(bool value)
    {
        treeHealthTalent = value;
    }

    private void TreeHealthTalentEnter()
    {
        if (treeHealthTalent && _currentTree != null) _treeHealthCoroutine = StartCoroutine(IncreaseTreeMaxHealthOverTime());
    }

    private void TreeHealthTalentExit()
    {
        if (_treeHealthCoroutine != null)
        {
            StopCoroutine(_treeHealthCoroutine);
            _treeHealthCoroutine = null;
        }

        treeData.MaxHealth = baseHealth;
        CastStreamDuration = baseCastStreamDuration;

        CmdSetMaxHealth(treeData.MaxHealth);
    }

    private IEnumerator IncreaseTreeMaxHealthOverTime()
    {
        float increaseDuration = 60f;
        float interval = 0.3f;
        int steps = Mathf.FloorToInt(increaseDuration / interval);

        for (int i = 0; i < steps; i++)
        {
            treeData.MaxHealth += 1;
            CmdSetMaxHealth(treeData.MaxHealth);

            if (_healthTree != null) _healthTree.ObjectData.MaxHealth = treeData.MaxHealth;

            yield return new WaitForSeconds(interval);
        }
    }
    #endregion

    #region Talent for Magical abbilities evade

    public void treeMagicEvadeTalentActive(bool value)
    {
        treeMagicEvadeTalent = value;
    }

    #endregion

    protected override void ClearData() => _targetPoint = Vector3.positiveInfinity;
    public override void LoadTargetData(TargetInfo targetInfo) => _targetPoint = targetInfo.Points[0];
}