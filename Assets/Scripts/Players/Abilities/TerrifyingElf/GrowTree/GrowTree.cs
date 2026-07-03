using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GrowTree : Skill
{
    [Header("GrowTree Settings")]
    [SerializeField] private float extendedRadius = 8f;
    [SerializeField] private float _moveDuration = 0.5f;
    [SerializeField] private float _arrowEffectLifetime = 2;
    [SerializeField] private GrowTreeAura _treePrefab;
    [SerializeField] private MoveComponent _moveComponent;
    [SerializeField] private List<GrowTreeAura> _activeTrees;
    [SerializeField] private ObjectData _treeData;
    [SerializeField] private DrawCircle _extendedRadiusCircle;
    [SerializeField] private Color _extendedRadiusColor = new Color(0.8f, 0.3f, 0f);
    [SerializeField] private ShotsIntoSky _shotsIntoSky;
    [SerializeField] private ShotIntoSky _shotIntoSky;
    [SerializeField] private GameObject _arrowWithTreeEffect;
    [SerializeField] private ParticleSystem _arrowIntoSkyEffect;
    [SerializeField] private SkillQueue _skillQueue;
    [SerializeField] private SkillManager _skillManager;

    [Header("Talents")]
    private bool _growTreeIncreasesMaxHealth;
    private bool _treeMagicEvadeTalent;
    private bool _treeShotCooldownTalent;
    private bool _isGrowTreeArrowIntoSkyRadiusTalent;

    [Header("Raycast masks")]
    [SerializeField] private LayerMask groundLayer;

    private Vector3 _targetPoint = Vector3.positiveInfinity;
    private GrowTreeAura _currentTree;
    private ObjectHealth _healthTree;
    private float _baseHealth;
    private float _baseCastDelay;
    private Coroutine _treeHealthCoroutine;
    private Coroutine _rangeWatch;
    private Coroutine _checkExtendedRadiusCoroutine;
    private Coroutine _arrowFxRoutine;
    private Coroutine _growVisualExitRoutine;
    private bool _isSpawnHero;
    private bool _castFromExtendedRadius;
    private WaitForSeconds _waitForExtendedRadiusInterval;

    #region Const
    private const float MaxMouseRaycastDistance = 1000f;
    private const float ExtendedRadiusCheckInterval = 0.1f;
    private const float AnimatorCrossFadeDuration = 0.1f;
    private const float TreeTeleportYOffset = 5f;
    private const float TreeFillDuration = 1f;
    private const float SearchMousClickTarget = 1f;
    private const float MagicEvade = 100f;


    private const string GrowTreeCastDelayExit = "GrowTreeCastDelayExit";
    private const string GrowTreeCastDelay = "GrowTreeCastDelay";
    private const string ShotSkyWithTreeCastDelay = "ShotSkyWithTreeCastDelay";
    #endregion

    protected override bool IsCanCast
    {
        get
        {
            if (float.IsPositiveInfinity(_targetPoint.x)) return false;

            float allowedRadius = _isGrowTreeArrowIntoSkyRadiusTalent ? extendedRadius : AreaInfo.Radius;
            return Targeting.IsPointInRadius(allowedRadius, _targetPoint);
        }
    }

    private int _growHash = Animator.StringToHash(GrowTreeCastDelay);
    private int _shotHash = Animator.StringToHash(ShotSkyWithTreeCastDelay);

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    public void ArrowIntoSkyWithTreeEffectPlay() => _arrowIntoSkyEffect.Play();

    public void GrowTreeStopMove()
    {
        _hero.Move.StopMoveAndAnimationMove();
        _hero.Move.IsMoveBlocked = true;
    }

    public void GrowTreeStartMove()
    {
        _hero.Move.IsMoveBlocked = false;
    }

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);
        _baseCastDelay = CastDeley;
        _baseHealth = _treeData.MaxHealth;
        _waitForExtendedRadiusInterval = new WaitForSeconds(ExtendedRadiusCheckInterval);
    }

    private void OnEnable()
    {
        OnSkillCanceled += HandleSkillCanceled;
        _skillQueue.Cancell += HandleSkillDeleted;
    }

    private void OnDisable()
    {
        OnSkillCanceled -= HandleSkillCanceled;
        _skillQueue.Cancell -= HandleSkillDeleted;
    }
    
    protected override void PlayPrepareAnim()
    {
        string trigger = _castFromExtendedRadius ? ShotSkyWithTreeCastDelay : GrowTreeCastDelay;
        Animation.PlayTrigger(trigger);
    }

    private void HandleSkillDeleted(Skill skill)
    {
        if (skill == this) Renderer.HideAOEIndicator(isCommand: false);
    }
    private void ShowExtendedRadius()
    {
        if (_extendedRadiusCircle == null) _extendedRadiusCircle = GetComponentInChildren<DrawCircle>(true);
    }

    private void HideExtendedRadius()
    {
        if (_extendedRadiusCircle != null) _extendedRadiusCircle.Clear();
    }

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
        _castFromExtendedRadius = false;
        CastDeley = _baseCastDelay;

        if (_arrowFxRoutine != null)
        {
            StopCoroutine(_arrowFxRoutine);
            _arrowFxRoutine = null;
        }
    }

    private IEnumerator CheckExtendedRadiusJob()
    {
        while (true)
        {
            if (_extendedRadiusCircle == null)
            {
                yield return null;
                continue;
            }

            Vector3 mousePoint = GetMousePointOnLayer(groundLayer);
            bool cursorInside = !float.IsPositiveInfinity(mousePoint.x) && Vector3.Distance(mousePoint, transform.position) <= extendedRadius;

            _extendedRadiusCircle.SetColor(cursorInside ? Color.green : _extendedRadiusColor);
            _extendedRadiusCircle.Draw(extendedRadius);

            yield return _waitForExtendedRadiusInterval;
        }
    }
    
    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TreeHealthTalentEnter();
        _activeTrees.RemoveAll(tree => tree == null);

        CmdRemoveTree();

        if (_isGrowTreeArrowIntoSkyRadiusTalent)
        {
            ShowExtendedRadius();
            if (_checkExtendedRadiusCoroutine != null) StopCoroutine(_checkExtendedRadiusCoroutine);
            _checkExtendedRadiusCoroutine = StartCoroutine(CheckExtendedRadiusJob());
        }

        int treeCount = _activeTrees.Count;
        CastDeley = treeCount == 0 ? _baseCastDelay : _baseCastDelay * Mathf.Pow(2, treeCount);

        Vector3 targetPoint = Vector3.positiveInfinity;

        while (float.IsPositiveInfinity(targetPoint.x))
        {
            if (GetMouseButton)
            {
                Vector3 mousePoint = Targeting.GetMousePoint();

                bool clickedOnHero = false;

                Collider[] colliders = Physics.OverlapSphere(mousePoint, SearchMousClickTarget);
                foreach (var collider in colliders)
                {
                    if (collider.TryGetComponent<Character>(out Character hitCharacter) && hitCharacter == _hero)
                    {
                        clickedOnHero = true;
                        break;
                    }
                }

                targetPoint = mousePoint;

                if (clickedOnHero)
                {
                    targetPoint = _hero.transform.position;
                    _isSpawnHero = true;
                }

                else
                {
                    float dist = Vector3.Distance(transform.position, targetPoint);
                    if (dist <= AreaInfo.Radius) _castFromExtendedRadius = false;
                }
            }

            yield return null;
        }

        int nearCount = _activeTrees.Count(tree => tree != null && Vector3.Distance(tree.transform.position, targetPoint) <= AreaInfo.Radius);
        CastDeley = _castFromExtendedRadius
            ? 0f
            : (nearCount == 0 ? _baseCastDelay : _baseCastDelay * Mathf.Pow(2, nearCount));

        CmdSetCastDelayByProximity(targetPoint, AreaInfo.Radius, _castFromExtendedRadius);

        if (_checkExtendedRadiusCoroutine != null)
        {
            StopCoroutine(_checkExtendedRadiusCoroutine);
            _checkExtendedRadiusCoroutine = null;
        }
        HideExtendedRadius();

        Renderer.ShowAOEIndicator(targetPoint, isCommand: false);

        if (_castFromExtendedRadius)
        {
            _hero.Animator.SetTrigger(_shotHash);
            _hero.NetworkAnimator.SetTrigger(_shotHash);
        }

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Points.Add(targetPoint);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (_treePrefab == null) yield break;

        Renderer.HideAOEIndicator(isCommand: false);

        if (_rangeWatch != null)
        {
            StopCoroutine(_rangeWatch);
            _rangeWatch = null;
        }

        bool wasFromExtendedRadius = _castFromExtendedRadius;
        Vector3 spawnPos = _targetPoint;

        if (_isSpawnHero) CmdSpawnTreeAndTeleport(_hero.transform.position, TreeFillDuration);
        else CmdSpawnTree(spawnPos, _castFromExtendedRadius, TreeFillDuration);

        if (_growVisualExitRoutine != null) StopCoroutine(_growVisualExitRoutine);
        _growVisualExitRoutine = StartCoroutine(FinishGrowthVisualAfterDelay(TreeFillDuration, wasFromExtendedRadius));

        GrowTreeStartMove();
        ResetData();
        StopRangeWatch();

        yield break;
    }

    private IEnumerator FinishGrowthVisualAfterDelay(float delay, bool wasFromExtendedRadius)
    {
        yield return new WaitForSeconds(delay);

        if (wasFromExtendedRadius)
        {
            _hero.Animator.ResetTrigger(_shotHash);
            _hero.NetworkAnimator.ResetTrigger(_shotHash);
        }
        else
        {
            _hero.Animator.ResetTrigger(_growHash);
            _hero.NetworkAnimator.ResetTrigger(_growHash);
            CmdCrossFade();
            _hero.Animator.CrossFade(GrowTreeCastDelayExit, AnimatorCrossFadeDuration);
        }

        _growVisualExitRoutine = null;
    }

    protected Vector3 GetMousePointOnLayer(LayerMask layer, float y = 0f)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, MaxMouseRaycastDistance, layer))
        {
            Vector3 point = hit.point;
            point.y = y;
            return point;
        }

        return Vector3.positiveInfinity;
    }

    #region Canceling a skill
    private void HandleSkillCanceled()
    {
        if (_checkExtendedRadiusCoroutine != null)
        {
            StopCoroutine(_checkExtendedRadiusCoroutine);
            _checkExtendedRadiusCoroutine = null;
        }
        HideExtendedRadius();

        if (_hero != null && _hero.Move != null) Hero.Animator.speed = 1;
        TreeHealthTalentExit();

        if (_currentTree != null) CmdRequestInterruptTree(_currentTree.netId);

        if (_castCoroutine != null)
        {
            StopCoroutine(_castCoroutine);
            _castCoroutine = null;
        }

        GrowTreeStartMove();
        ResetData();
        StopRangeWatch();
    }
    #endregion

    #region [Command] / Spawn
    [Command] private void CmdSetMaxHealth(float maxHealth) => _treeData.MaxHealth = maxHealth;
    [Command] private void CmdRemoveTree() => _activeTrees.RemoveAll(tree => tree == null);

    [Command]
    private void CmdSpawnTree(Vector3 position, bool castFromExtendedRadius, float remainingDuration)
    {
        var tree = Instantiate(_treePrefab, position, Quaternion.identity);
        _currentTree = tree;

        tree.Init(_skillManager, Hero);
        NetworkServer.Spawn(_currentTree.gameObject, connectionToClient);

        _healthTree = tree.GetComponentInChildren<ObjectHealth>();
        if (_healthTree != null)
        {
            if (_treeData.MinEndurance) _healthTree.ServerStartFillHP(_healthTree.ObjectData.MaxHealth, remainingDuration);

            if (_treeMagicEvadeTalent) _healthTree.SetMagicEvade(MagicEvade);
        }
        ResetShotCooldowns();

        _activeTrees.Add(tree);
        _currentTree.GrowTreeIncreasesMaxHealth = _growTreeIncreasesMaxHealth;
        RpcClientAddTree(tree.GetComponent<NetworkIdentity>().netId, _currentTree);
    }

    [Command]
    private void CmdSpawnTreeAndTeleport(Vector3 position, float remainingDuration)
    {
        Vector3 spawnPosition = position;

        var tree = Instantiate(_treePrefab, spawnPosition, Quaternion.identity);
        _currentTree = tree;

        tree.Init(_skillManager, Hero);
        NetworkServer.Spawn(_currentTree.gameObject, connectionToClient);

        RpcTeleportToTree(_currentTree.gameObject);

        _healthTree = _currentTree.GetComponentInChildren<ObjectHealth>();
        if (_healthTree != null)
        {
            if (_treeData.MinEndurance) _healthTree.ServerStartFillHP(_healthTree.ObjectData.MaxHealth, remainingDuration);

            if (_treeMagicEvadeTalent) _healthTree.SetMagicEvade(MagicEvade);
        }
        ResetShotCooldowns();

        _activeTrees.Add(tree);
        _currentTree.GrowTreeIncreasesMaxHealth = _growTreeIncreasesMaxHealth;
        RpcClientAddTree(tree.GetComponent<NetworkIdentity>().netId, _currentTree);
    }

    [Command]
    private void CmdRequestInterruptTree(uint treeNetId)
    {
        if (NetworkServer.spawned.TryGetValue(treeNetId, out NetworkIdentity networkIdentity))
        {
            var health = networkIdentity.GetComponentInChildren<ObjectHealth>();
            health.ServerInterruptFillHP();
        }
    }

    [Command]
    private void CmdCrossFade()
    {
        _hero.Animator.CrossFade(GrowTreeCastDelayExit, AnimatorCrossFadeDuration);
    }

    [Command]
    private void CmdSetCastDelayByProximity(Vector3 plannedPos, float checkRadius, bool castFromExtendedRadius)
    {
        _activeTrees.RemoveAll(tree => tree == null);

        if (castFromExtendedRadius)
        {
            CastDeley = 0f;
            return;
        }

        int nearCount = 0;
        foreach (var tree in _activeTrees)
            if (tree != null && Vector3.Distance(tree.transform.position, plannedPos) <= checkRadius) nearCount++;

        CastDeley = nearCount == 0 ? _baseCastDelay : _baseCastDelay * Mathf.Pow(2, nearCount);
    }

    [ClientRpc]
    private void RpcClientAddTree(uint netId, GrowTreeAura currentTree)
    {
        _currentTree = currentTree;
        _currentTree.GrowTreeIncreasesMaxHealth = _growTreeIncreasesMaxHealth;
        _currentTree.Init(_skillManager, Hero);
        if (NetworkClient.spawned.TryGetValue(netId, out var networkIdentity)) _activeTrees.Add(networkIdentity.GetComponent<GrowTreeAura>());
    }

    [ClientRpc]
    private void RpcTeleportToTree(GameObject tree)
    {
        if (tree != null)
        {
            Vector3 topOfTree = tree.transform.position + Vector3.up * TreeTeleportYOffset;
            _moveComponent.TeleportToPositionSmooth(topOfTree, _moveDuration);
        }
    }

    [ClientRpc]
    private void ResetShotCooldowns()
    {
        if (!_treeShotCooldownTalent) return;

        if (_shotsIntoSky != null && _shotsIntoSky.Cooldown.IsActive) _shotsIntoSky.Cooldown.ForceEnd();
        if (_shotIntoSky != null && _shotIntoSky.Cooldown.IsActive) _shotIntoSky.Cooldown.ForceEnd();
    }
    #endregion

    #region Talent
    public void ShotTreeCooldownTalent(bool value) => _treeShotCooldownTalent = value;
    public void GrowTreeArrowIntoSkyRadius(bool value) => _isGrowTreeArrowIntoSkyRadiusTalent = value;
    #endregion

    #region Talent for doubling HP
    public void treeHealthTalentActive(bool value)
    {
        //treeHealthTalent = value;
        _growTreeIncreasesMaxHealth = value;
    }

    private void TreeHealthTalentEnter()
    {
        //if (treeHealthTalent && _currentTree != null) _treeHealthCoroutine = StartCoroutine(IncreaseTreeMaxHealthOverTime());
    }

    private void TreeHealthTalentExit()
    {
        if (_treeHealthCoroutine != null)
        {
            StopCoroutine(_treeHealthCoroutine);
            _treeHealthCoroutine = null;
        }

        _treeData.MaxHealth = _baseHealth;
        //Channeling.CastDuration = _baseCastStreamDuration;

        CmdSetMaxHealth(_treeData.MaxHealth);
    }
    #endregion

    #region Talent for Magical abbilities evade

    public void treeMagicEvadeTalentActive(bool value)
    {
        _treeMagicEvadeTalent = value;
    }

    #endregion

    protected override void ClearData() => _targetPoint = Vector3.positiveInfinity;
    public override void LoadTargetData(TargetInfo targetInfo) => _targetPoint = targetInfo.Points[0];
}
