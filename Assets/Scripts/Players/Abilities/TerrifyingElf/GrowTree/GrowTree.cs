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

    private ShotsIntoSky _shotsIntoSky;
    private ShotIntoSky _shotIntoSky;

    protected override bool IsCanCast =>
        !float.IsPositiveInfinity(_targetPoint.x) &&
        IsPointInRadius(Radius, _targetPoint);

    protected override int AnimTriggerCastDelay => Animator.StringToHash("SpellCastDelayAnimTrigger");
    protected override int AnimTriggerCast => 0;

    private void Start()
    {
        SkillManager skillManager = Hero.Abilities;

        if (_shotsIntoSky == null) _shotsIntoSky = skillManager.Abilities.OfType<ShotsIntoSky>().FirstOrDefault();
        if (_shotIntoSky == null) _shotIntoSky = skillManager.Abilities.OfType<ShotIntoSky>().FirstOrDefault();

        baseHealth = treeData.MaxHealth;
        baseCastStreamDuration = CastStreamDuration;
    }

    private void OnEnable()
    {
        OnSkillCanceled += HandleSkillCanceled;
        CastSuccess += HandleSkillCanceled;
    }

    private void OnDestroy()
    {
        OnSkillCanceled -= HandleSkillCanceled;
        CastSuccess -= HandleSkillCanceled;
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
                var clickedCharacter = GetClickedCharacter();
                if (clickedCharacter != null && clickedCharacter == _hero) _targetPoint = _hero.transform.position;
                else
                {
                    _targetPoint = GetMousePoint();
                    Hero.Move.CanMove = false;
                    Hero.Move.LookAtPosition(_targetPoint);
                    if (!IsPointInRadius(Radius, _targetPoint)) _targetPoint = Vector3.positiveInfinity;
                }
            }
            yield return null;
        }

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Points.Add(_targetPoint);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (_treePrefab == null) yield break;

        moveComponent.CanMove = false;

        _hero.Animator.SetTrigger(AnimTriggerCastDelay);
        _hero.NetworkAnimator.SetTrigger(AnimTriggerCastDelay);

        yield return new WaitForSeconds(CastStreamDuration / 3);

        var clickedCharacter = GetClickedCharacter();
        if (clickedCharacter != null && clickedCharacter == _hero) CmdSpawnTreeAndTeleport(_hero.transform.position);
        else CmdSpawnTree(_targetPoint);
    }

    #region Canceling a skill
    private void HandleSkillCanceled()
    {
        if (_hero != null && _hero.Move != null)
        {
            Hero.Animator.speed = 1;
            Hero.Move.CanMove = true;
            Hero.Move.StopLookAt();
        }

        TreeHealthTalentExit();

        if (_healthTree != null)
        {
            _healthTree.ServerStopFillHP();
            _healthTree = null;
        }
    }
    #endregion

    #region Auxiliary methods
    private Character GetClickedCharacter()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.TryGetComponent(out Character character))
            {
                return character;
            }
        }
        return null;
    }
    #endregion

    [Server]
    private void OnTreeFullyRegenerated()
    {
        if (!treeShotCooldownTalent) return;

        ResetShotCooldown(_shotsIntoSky);
        ResetShotCooldown(_shotIntoSky);
    }

    private void ResetShotCooldown(Skill shotSkill)
    {
        if (shotSkill == null || shotSkill.IsCooldowned) return;

        float reset = shotSkill.RemainingCooldownTime;

        shotSkill.ReductionSetCooldown(reset);

        if (shotSkill is ShotsIntoSky shots) shots.TargetResetCooldown(reset);
        if (shotSkill is ShotIntoSky shot) shot.TargetResetCooldown(reset);
    }

    #region [Command] / Spawn
    [Command] private void CmdSetMaxHealth(float maxHealth) => treeData.MaxHealth = maxHealth;
    [Command] private void CmdRemoveTree() => _activeTrees.RemoveAll(t => t == null);

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

        _activeTrees.Add(tree);
        RpcClientAddTree(tree.GetComponent<NetworkIdentity>().netId);
    }

    [Command]
    private void CmdSpawnTreeAndTeleport(Vector3 position)
    {
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

        _activeTrees.Add(tree);
        RpcClientAddTree(tree.GetComponent<NetworkIdentity>().netId);
    }

    [Command]
    private void CmdCastStreamDurationWithTree()
    {
        int treeCount = _activeTrees.Count;
        CastStreamDuration = treeCount == 0 ? baseCastStreamDuration : baseCastStreamDuration * Mathf.Pow(2, treeCount);
    }

    [ClientRpc]
    private void RpcClientAddTree(uint netId)
    {
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
