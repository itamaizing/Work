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

    [Header("Talents")]
    //[SerializeField] private bool treeHealthTalent; // Созданное дерево каждые 0,3 сек увеличивает максималньый запас здоровья на 1 ед. Вплоть до 60 сек.
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
    private float _baseCastStreamDuration;
    private float _baseCastDelay;
    private Coroutine _treeHealthCoroutine;
    private Coroutine _rangeWatch;
    private Coroutine _checkExtendedRadiusCoroutine;
    private Coroutine _arrowFxRoutine;
    private Coroutine _streamCoroutine;
    private bool _isSpawnHero;
    private bool _castFromExtendedRadius;
    private bool _streamFinished;
    private WaitForSeconds _waitForExtendedRadiusInterval;
    private WaitForSeconds _waitForCastStreamDurationFirst;
    private WaitForSeconds _waitForCastStreamDurationSecond;
    private WaitForSeconds _waitForCastStreamDurationThird;

    #region Const
    private const float MaxMouseRaycastDistance = 1000f;
    private const float ExtendedRadiusCheckInterval = 0.1f;
    private const float AnimatorCrossFadeDuration = 0.1f;
    private const float TreeTeleportYOffset = 5f;
    private const float CastStreamDurationFirst = 3f;
    private const float CastStreamDurationSecond = 1.5f;
    private const float SearchRadiusTarget = 1f;
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
            return IsPointInRadius(allowedRadius, _targetPoint);
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

    private void Start()
    {
        _baseCastDelay = CastDeley;
        _baseHealth = _treeData.MaxHealth;
        _baseCastStreamDuration = CastStreamDuration;
        _waitForExtendedRadiusInterval = new WaitForSeconds(ExtendedRadiusCheckInterval);
        _waitForCastStreamDurationFirst = new WaitForSeconds(CastStreamDuration / CastStreamDurationFirst);
        _waitForCastStreamDurationSecond = new WaitForSeconds(CastStreamDuration / CastStreamDurationSecond);
        _waitForCastStreamDurationThird = new WaitForSeconds(CastStreamDuration);
    }

    private void OnEnable()
    {
        OnSkillCanceled += HandleSkillCanceled;
        _skillQueue.Cancell += HandleSkillDeleted;
    }
    private void OnDisable ()
    {
        OnSkillCanceled -= HandleSkillCanceled;
        _skillQueue.Cancell -= HandleSkillDeleted;
    }

    private void HandleSkillDeleted(Skill skill)
    {
        if (skill == this) ClientStopDamageZone();
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

    //private void SpawnArrowWithTreeEffect(Vector3 point)
    //{
    //    if (!_arrowWithTreeEffect) return;

    //    Vector3 direction = point - transform.position;
    //    direction.y = 0f;
    //    Quaternion rotation = direction.sqrMagnitude > ArrowLookMinThresholdSqr ? Quaternion.LookRotation(direction) : Quaternion.identity;

    //    var effect = Instantiate(_arrowWithTreeEffect, point, rotation);
    //    SceneManager.MoveGameObjectToScene(effect, gameObject.scene);
    //    Destroy(effect, _arrowEffectLifetime);
    //}

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

    //private IEnumerator ISpawnArrowWithTreeEffect()
    //{
    //    yield return new WaitForSeconds(CastStreamDuration / 5);
    //    if (_castFromExtendedRadius) SpawnArrowWithTreeEffect(_targetPoint);
    //}

    private IEnumerator CheckExtendedRadiusJob()
    {
        while (true)
        {
            if (_extendedRadiusCircle == null)
            {
                yield return null;
                continue;
            }

            //float extRadius = (_shotIntoSky != null) ? _shotIntoSky.AreaInfo.Radius : 0f;

            //if (extRadius <= 0f)
            //{
            //    _extendedRadiusCircle.Clear();
            //    yield return null;
            //    continue;
            //}

            Vector3 mousePoint = GetMousePointOnLayer(groundLayer);
            bool cursorInside = !float.IsPositiveInfinity(mousePoint.x) && Vector3.Distance(mousePoint, transform.position) <= extendedRadius;

            _extendedRadiusCircle.SetColor(cursorInside ? Color.green : _extendedRadiusColor);
            _extendedRadiusCircle.Draw(extendedRadius);

            yield return _waitForExtendedRadiusInterval;
        }
    }

    //private IEnumerator CastDistanceWatcher()
    //{
    //    const float checkInterval = 0.1f;
    //    var wait = new WaitForSeconds(checkInterval);

    //    try
    //    {
    //        while (true)
    //        {
    //            if (_hero == null) yield break;

    //            float allowed = _castFromExtendedRadius ? extendedRadius : AreaInfo.Radius;
    //            float allowedSqr = allowed * allowed;

    //            Vector3 heroPos = _hero.transform.position;
    //            Vector3 anchor = _currentTree != null ? _currentTree.transform.position : _targetPoint;

    //            if ((heroPos - anchor).sqrMagnitude > allowedSqr)
    //            {
    //                TryCancel();
    //                ResetData();
    //                break;
    //            }

    //            yield return wait;
    //        }
    //    }
    //    finally
    //    {
    //        _rangeWatch = null;
    //    }
    //}
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
        Channeling.CastDuration = treeCount == 0 ? _baseCastStreamDuration : _baseCastStreamDuration * Mathf.Pow(2, treeCount);

        Vector3 targetPoint = Vector3.positiveInfinity;

        while (float.IsPositiveInfinity(targetPoint.x))
        {
            if (GetMouseButton)
            {
                Vector3 mousePoint = GetMousePoint();
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

                    //else if (dist <= extendedRadius && _isGrowTreeArrowIntoSkyRadiusTalent)
                    //{
                    //    if (shotIntoSky != null && !shotIntoSky.IsCooldowned && !shotIntoSky.Disactive)
                    //    {
                    //        yield return null;
                    //        continue;
                    //    }

                    //    _castFromExtendedRadius = true;
                    //    CastDeley += arrowEffectLifetime;
                    //    SpawnArrowWithTreeEffect(targetPoint);

                    //    if (shotIntoSky != null && shotIntoSky.IsUseCharges) shotIntoSky.TryUseCharge();
                    //    else if (shotIntoSky != null) shotIntoSky.IncreaseSetCooldown(shotIntoSky.CooldownTime);
                    //}
                }
            }

            yield return null;
        }

        int nearCount = _activeTrees.Count(tree => tree != null && Vector3.Distance(tree.transform.position, targetPoint) <= AreaInfo.Radius);
        Channeling.CastDuration = nearCount == 0 ? _baseCastStreamDuration : _baseCastStreamDuration * Mathf.Pow(2, nearCount);

        CmdSetCastStreamDurationByProximity(targetPoint, AreaInfo.Radius);

        if (_checkExtendedRadiusCoroutine != null)
        {
            StopCoroutine(_checkExtendedRadiusCoroutine);
            _checkExtendedRadiusCoroutine = null;
        }
        HideExtendedRadius();

        DrawDamageZoneClient(targetPoint);

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

        GrowTreeStopMove();
        if (_streamCoroutine != null) StopCoroutine(_streamCoroutine);

        _streamCoroutine = StartCoroutine(StreamDuration());
        ClientStopDamageZone();

        if (_rangeWatch != null)
        {
            StopCoroutine(_rangeWatch);
            _rangeWatch = null;
        }

        Vector3 spawnPos = _targetPoint;


        if (!_castFromExtendedRadius)
        {
            _hero.Animator.SetTrigger(_growHash);
            _hero.NetworkAnimator.SetTrigger(_growHash);

            yield return _waitForCastStreamDurationFirst;
        }

        if (_isSpawnHero) CmdSpawnTreeAndTeleport(_hero.transform.position);
        else CmdSpawnTree(spawnPos, _castFromExtendedRadius);


        if (!_castFromExtendedRadius) yield return _waitForCastStreamDurationSecond;
        else yield return _waitForCastStreamDurationThird;

        while (!_streamFinished) yield return null;
    }

    private IEnumerator StreamDuration()
    {
        _streamFinished = false;
        yield return new WaitForSeconds(CastStreamDuration);

        if (_castFromExtendedRadius)
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

        GrowTreeStartMove();
        ResetData();
        StopRangeWatch();

        _streamFinished = true;
        _streamCoroutine = null;
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

        if (_streamCoroutine != null)
        {
            StopCoroutine(_streamCoroutine);
            _streamCoroutine = null;
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
    private void CmdSpawnTree(Vector3 position, bool castFromExtendedRadius)
    {
        var tree = Instantiate(_treePrefab, position, Quaternion.identity);
        _currentTree = tree;

        SceneManager.MoveGameObjectToScene(_currentTree.gameObject, Hero.NetworkSettings.MyRoom);
        NetworkServer.Spawn(_currentTree.gameObject, connectionToClient);

        _healthTree = tree.GetComponentInChildren<ObjectHealth>();
        if (_healthTree != null)
        {
            float regenDuration = 0;
            if (!castFromExtendedRadius) regenDuration = CastStreamDuration - CastStreamDuration / CastStreamDurationFirst;
            else regenDuration = CastStreamDuration;

            _healthTree.InitializeObject(_treeData);
            if (_treeData.MinEndurance) _healthTree.ServerStartFillHP(_healthTree.ObjectData.MaxHealth, regenDuration);

            if (_treeMagicEvadeTalent) _healthTree.SetMagicEvade(MagicEvade);

        }
        ResetShotCooldowns();

        _activeTrees.Add(tree);
        _currentTree.GrowTreeIncreasesMaxHealth = _growTreeIncreasesMaxHealth;
        RpcClientAddTree(tree.GetComponent<NetworkIdentity>().netId, _currentTree);
    }

    [Command]
    private void CmdSpawnTreeAndTeleport(Vector3 position)
    {
        Debug.Log($"TargetPoint: {_targetPoint.x}");
        Vector3 spawnPosition = position;

        var tree = Instantiate(_treePrefab, spawnPosition, Quaternion.identity);
        _currentTree = tree;
        NetworkServer.Spawn(_currentTree.gameObject, connectionToClient);
        SceneManager.MoveGameObjectToScene(_currentTree.gameObject, Hero.NetworkSettings.MyRoom);

        RpcTeleportToTree(_currentTree.gameObject);

        _healthTree = _currentTree.GetComponentInChildren<ObjectHealth>();
        if (_healthTree != null)
        {
            _healthTree.InitializeObject(_treeData);

            float regenDuration = CastStreamDuration - CastStreamDuration / CastStreamDurationFirst;

            if (_treeData.MinEndurance) _healthTree.ServerStartFillHP(_healthTree.ObjectData.MaxHealth, regenDuration);

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
    private void CmdSetCastStreamDurationByProximity(Vector3 plannedPos, float checkRadius)
    {
        _activeTrees.RemoveAll(tree => tree == null);

        int nearCount = 0;
        foreach (var tree in _activeTrees) if (tree != null && Vector3.Distance(tree.transform.position, plannedPos) <= checkRadius) nearCount++;
        Channeling.CastDuration = nearCount == 0 ? _baseCastStreamDuration : _baseCastStreamDuration * Mathf.Pow(2, nearCount);
    }

    [ClientRpc]
    private void RpcClientAddTree(uint netId, GrowTreeAura currentTree)
    {
        _currentTree = currentTree;
        _currentTree.GrowTreeIncreasesMaxHealth = _growTreeIncreasesMaxHealth;
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

        if (_shotsIntoSky != null && !_shotsIntoSky.IsCooldowned) _shotsIntoSky.ForceCooldownEnd();
        if (_shotIntoSky != null && !_shotIntoSky.IsCooldowned) _shotsIntoSky.ForceCooldownEnd();
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
        Channeling.CastDuration = _baseCastStreamDuration;

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