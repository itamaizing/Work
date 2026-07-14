using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class Ghost : Skill
{
    [Header("Ghost Settings")]
    [SerializeField] private float extendedRadius = 2f;
    [SerializeField] private float baseRadius = 3f;
    [SerializeField] private float teleportManaUse = 6f;
    [SerializeField] private int maxGhosts = 2;
    [SerializeField] private MinionComponent ghostPrefab;
    [SerializeField] private GameObject ghostPrefabPreview;
    [SerializeField] private GameObject way;
    [SerializeField] private AudioClip aCTeleportToGhost;
    [SerializeField] private AudioClip aCСontrolGhostToTarget;
    [SerializeField] private AudioClip aCSummoningGhost;
    [SerializeField] private DrawCircle _extendedRadiusCircle;
    [SerializeField] private Color extendedRadiusColor = new Color(0.8f, 0.3f, 0f);
    [SerializeField] private List<Character> _ghosts;
    [SerializeField] private VisionComponent treeVisionComponent;
    [SerializeField] private VisionComponent heroVisionComponent;
    [SerializeField] private SkillQueue skillQueue;

    private GameObject _ghostPrefabPreview;
    private AudioSource _audioSource;
    private SpawnComponent _spawnComponent;
    private float _ghostPrepearCount;
    private float _baseCastDelay;
    private float _treeVisionRadius;
    private float _heroVisionRadius;
    private float _infinityDistance = 999;
    private bool _isPreviewHiddenOverGhost;
    private bool _ghostMoveToTarget;
    private bool _teleportGhost;
    private bool _isSpawningGhostVisual;
    private Vector3 _spawnPosition;
    private Character _ghostToMove;
    private Character _targetCharacter;
    private Character _ghostToTeleport;
    private Coroutine _checkExtendedRadiusCoroutine;
    private Coroutine _teleportAnimationCoroutine;
    private Coroutine _boostWindow;
    private List<GrowTreeAura> _allGrowTrees = new();

    private const float ManaPercentToCheckTeleport = 0.05f;

    private Resource _manaResource;

    private readonly Queue<Vector3> _pendingSpawn = new();

    #region Talent
    private bool _sendingGhostTargetTalentActive;
    private bool _cooldownGhostShotActive;
    private bool _effectsInnerDarknessTalent;
    private bool _movingToGhostWithZeroMana;
    private bool _passingThroughGhost;
    private bool _isPullingHealthGostTeleport;
    private bool _isGhostSpawnInRadiusTree;
    #endregion

    private bool isSkillEnableBoostLogic;

    public event Action<Character, Vector3> Teleported;

    protected override int AnimTriggerCastDelay => Animator.StringToHash("GhostCastDelay");
    protected override int AnimTriggerCast => 0;

    protected override bool IsCanCast
    {
        get
        {
            if (_teleportGhost && _ghostToTeleport != null)
            {
                if (!_isGhostSpawnInRadiusTree) 
                    return IsWithinRadius(_ghostToTeleport.transform.position, AreaInfo.Radius + extendedRadius);
                
                return true; 
            }

            if (_ghostMoveToTarget) return true;

            if (!float.IsPositiveInfinity(_spawnPosition.x))
            {
                bool allowedByTree = _isGhostSpawnInRadiusTree && (IsNearGrowTree(_spawnPosition, 1f) || IsVisibleToHero(_spawnPosition));
                bool inRadius = IsWithinRadius(_spawnPosition, AreaInfo.Radius);

                if (!allowedByTree && !inRadius) 
                    return false;
            }

            if (Charges.HasCharges && (_chargesHaveSeparateCooldown || !Cooldown.IsActive)) return true;

            return false;
        }
    }
    
    public override bool IsHaveResources
    {
        get{        
            if (_teleportGhost) 
                return true;

            return base.IsHaveResources;
        }
    }
    
    public bool CooldownGhostShotActive => _cooldownGhostShotActive;
    public List<Character> GhostTarget { get => _ghosts; set => _ghosts = value; }

    #region Talents
    public void EffectsInnerDarknessTalentActive(bool value) => _effectsInnerDarknessTalent = value;
    public void SendingGhostTargetTalentActive(bool value) => _sendingGhostTargetTalentActive = value;
    public void CooldownGhostShotActiveTalent(bool value) => _cooldownGhostShotActive = value;

    public void MovingToGhostWithZeroMana(bool value)
    {
        if(_movingToGhostWithZeroMana == value) return;
        
        _movingToGhostWithZeroMana = value;
        
        if (_movingToGhostWithZeroMana)
        {
            _manaResource.ValueChanged -= CheckForMana;
            _manaResource.ValueChanged += CheckForMana;
        }
        else
        {
            _manaResource.ValueChanged -= CheckForMana;
        }
    }

    public void PassingThroughGhost(bool value) => _passingThroughGhost = value;
    public void PullingHealthGostTeleport(bool value) => _isPullingHealthGostTeleport = value;
    public void GhostSpawnInRadiusTree(bool value) => _isGhostSpawnInRadiusTree = value;
    #endregion

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);

        _audioSource = GetComponent<AudioSource>();
        _treeVisionRadius = treeVisionComponent.VisionRange;
        _heroVisionRadius = Hero.VisionComponent.VisionRange;

        InitializeFields();
        RegisterSpawnEvents();

        _manaResource = _hero.Resources[ResourceType.Mana];
        
        AreaInfo.Radius = baseRadius;
        if (_extendedRadiusCircle == null) _extendedRadiusCircle = GetComponentInChildren<DrawCircle>(true);
    }

    private void CheckForMana(float oldValue, float newValue)
    {
        if (newValue < _manaResource.MaxValue * ManaPercentToCheckTeleport)
            EnableSkillBoost();
        else
            DisableSkillBoost();
    }
    
    protected override void SkillEnableBoostLogic() => isSkillEnableBoostLogic = true;
    protected override void SkillDisableBoostLogic() => isSkillEnableBoostLogic = false;

    private void OnEnable()
    {
        PreparingSuccess += OnPreparingConcluded;
        PreparingCanceled += OnPreparingConcludedNoArgs;
    }

    private void OnDisable()
    {
        PreparingSuccess -= OnPreparingConcluded;
        PreparingCanceled -= OnPreparingConcludedNoArgs;

        UnregisterSpawnEvents();
        if (_checkExtendedRadiusCoroutine != null)
        {
            StopCoroutine(_checkExtendedRadiusCoroutine);
            _checkExtendedRadiusCoroutine = null;
        }
    }
    
    private void OnPreparingConcluded(Skill skill) => HideExtendedRadiusAndStopWatch();
    private void OnPreparingConcludedNoArgs() => HideExtendedRadiusAndStopWatch();

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        base.LoadTargetData(targetInfo); 

        _teleportGhost = false;
        _ghostMoveToTarget = false;
        _targetCharacter = null;
        _ghostToTeleport = null;
        _spawnPosition = Vector3.positiveInfinity;

        if (targetInfo.GetTargets().Count > 0)
        {
            Character target = (Character)targetInfo.GetTargets()[0];
        
            if (_ghosts.Contains(target) || target.GetComponent<GhostAura>() != null)
            {
                _teleportGhost = true;
                _ghostToTeleport = target;
                _castDeley = 0f;
            }
            else
            {
                _ghostMoveToTarget = true;
                _targetCharacter = target;
                if (_ghosts.Count > 0)
                {
                    _ghostToMove = _ghosts.Count > 1 ? _ghosts[_ghosts.Count - 2] : _ghosts[0];
                }
            }
        }
        else if (targetInfo.Points.Count > 0)
        {
            _spawnPosition = targetInfo.Points[0];
        }
    }
    
    private void HideExtendedRadiusAndStopWatch()
    {
        HideExtendedRadius();
        if (_checkExtendedRadiusCoroutine != null)
        {
            StopCoroutine(_checkExtendedRadiusCoroutine);
            _checkExtendedRadiusCoroutine = null;
        }
    }

    public void TryStartGhostBoostWindow() => _boostWindow = StartCoroutine(GhostBoostWindow());

    private void HideExtendedRadius()
    {
        if (_extendedRadiusCircle != null) _extendedRadiusCircle.Clear();
    }

    private void InitializeFields()
    {
        _baseCastDelay = CastDeley;
        _ghosts = new List<Character>();
        _spawnComponent = GetComponent<SpawnComponent>();
    }

    private bool IsNearGrowTree(Vector3 point, float radius = 1f)
    {
        var hits = Physics.OverlapSphere(point, radius);
        for (int i = 0; i < hits.Length; i++) if (hits[i].GetComponentInParent<GrowTreeAura>() != null) return true;
        return false;
    }

    private void RegisterSpawnEvents()
    {
        if (_spawnComponent != null) _spawnComponent.UnitAdded += OnGhostSpawned;
    }

    private void UnregisterSpawnEvents()
    {
        if (_spawnComponent != null) _spawnComponent.UnitAdded -= OnGhostSpawned;
    }

    private IEnumerator GhostBoostWindow()
    {
        EnableSkillBoost();
        yield return new WaitForSeconds(3f);
        DisableSkillBoost();
        _boostWindow = null;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        if (_checkExtendedRadiusCoroutine != null) StopCoroutine(_checkExtendedRadiusCoroutine);
        if (!_isGhostSpawnInRadiusTree) _checkExtendedRadiusCoroutine = StartCoroutine(CheckExtendedRadiusJob());
        else _allGrowTrees = FindObjectsOfType<GrowTreeAura>().ToList();

        Vector3 mousePositionStart = Targeting.GetMousePoint();
        _ghostPrefabPreview = Instantiate(ghostPrefabPreview, mousePositionStart, Quaternion.identity);
        _isPreviewHiddenOverGhost = false;

        Vector3 secondPoint = Vector3.positiveInfinity;
        Character targetCharacter = null;
        Character targetGhost = null;

        while (true)
        {
            Vector3 firstPoint = Targeting.GetMousePoint();
            _teleportGhost = false;
            bool isHoveringGhost = IsMouseOverGhost(out Character ghostPreview) && ghostPreview.GetComponent<GhostAura>();

            if (_ghostPrefabPreview)
            {
                if (isHoveringGhost && !_isPreviewHiddenOverGhost)
                {
                    _ghostPrefabPreview.SetActive(false);
                    _isPreviewHiddenOverGhost = true;
                }
                else if (!isHoveringGhost && _isPreviewHiddenOverGhost)
                {
                    _ghostPrefabPreview.SetActive(true);
                    _isPreviewHiddenOverGhost = false;
                }

                if (_ghostPrefabPreview.activeSelf) _ghostPrefabPreview.transform.position = firstPoint;
            }

            if (_sendingGhostTargetTalentActive && IsMouseOverTarget(out Character character) && character.CharacterState.CheckForState(States.InnerDarkness))
            {
                if (GetMouseButton && IsWithinRadius(character.transform.position, AreaInfo.Radius) && !GetComponent<GhostAura>())
                {
                    if (_ghosts.Count > 0)
                    {
                        _ghostToMove = _ghosts.Count > 1 ? _ghosts[_ghosts.Count - 2] : _ghosts[0];
                        targetCharacter = character;
                        _ghostMoveToTarget = true;
                        break;
                    }
                }
            }
            else if (isHoveringGhost && !Hero.CharacterState.CheckForState(States.Bound))
            {
                if (IsCasting || _isSpawningGhostVisual)
                {
                    yield return null;
                    continue;
                }

                if (GetMouseButton)
                {
                    _teleportGhost = true;
                    targetGhost = ghostPreview;
                    break;
                }
            }
            else
            {
                if (GetMouseButton && !IsMouseOverTarget(out _))
                {
                    secondPoint = Targeting.GetMousePoint();
                    if (secondPoint == Vector3.zero) { yield return null; continue; }

                    if (isSkillEnableBoostLogic)
                    {
                        if (_isSpawningGhostVisual) _pendingSpawn.Enqueue(secondPoint);
                        else StartCoroutine(SpawnGhostVisualEffect(secondPoint));

                        yield return new WaitForSeconds(0.1f);
                        yield return null;
                    }
                    else
                    {
                        if (_ghostPrepearCount <= maxGhosts) _ghostPrepearCount++;
                        AdjustCastDelay();
                        break;
                    }
                }
            }

            yield return null;
        }

        if (_ghostPrefabPreview != null) Destroy(_ghostPrefabPreview);

        TargetInfo targetInfo = new TargetInfo();
        if (targetCharacter != null) targetInfo.AddTarget(targetCharacter);
        else if (targetGhost != null) targetInfo.AddTarget(targetGhost);
        else if (secondPoint != Vector3.positiveInfinity) targetInfo.Points.Add(secondPoint);

        while (GetMouseButton)
        {
            yield return null;
        }
        
        callbackDataSaved(targetInfo);
    }

    #region SendingGhostTarget
    private bool IsMouseOverTarget(out Character target)
    {
        target = null;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _targetsLayers))
        {
            target = hit.collider.GetComponent<Character>();
            return target != null;
        }

        return false;
    }

    private IEnumerator MoveGhostToCharacter(Character ghost, Character target)
    {
        if (ghost == null || target == null) yield break;
        if (!(ghost is MinionComponent minion)) yield break;
        if (!ghost.TryGetComponent<NavMeshAgent>(out var agent)) yield break;
        if (!TryConsumeMana(teleportManaUse)) yield break;

        agent.stoppingDistance = 1.5f;
        agent.updateRotation = true;

        Vector3 targetPosition = target.transform.position;
        targetPosition.y = 1f;
        agent.SetDestination(target.transform.position);

        while (true)
        {
            if (target == null)
            {
                agent.ResetPath();
                yield break;
            }

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    OnGhostReachedTarget(minion, target);
                    yield break;
                }
            }

            agent.SetDestination(target.transform.position);
            _ghostMoveToTarget = false;

            yield return null;
        }
    }

    private void OnGhostReachedTarget(Character ghost, Character target)
    {
        if (ghost == null || target == null) return;
        if (!(ghost is MinionComponent minion)) return;

        var characterState = target.GetComponent<CharacterState>();
        if (characterState != null)
        {
            int innerDarknessStacks = characterState.CheckStateStacks(States.InnerDarkness);

            if (innerDarknessStacks > 0)
            {
                float randomFearDuration = UnityEngine.Random.Range(0.4f, 0.6f) * innerDarknessStacks;
                characterState.AddState(States.Fear, randomFearDuration, 0, gameObject, "Ghost");
            }
            else if (innerDarknessStacks == 0) 
            {
                characterState.AddState(States.Fear, UnityEngine.Random.Range(0.4f, 0.6f), 0, gameObject, "Ghost");
            }
        }

        CmdAcСontrolGhostToTarget();
        _ghosts.Remove(ghost);
        Destroy(ghost.gameObject);
    }
    #endregion

    private void AdjustCastDelay()
    {
        if (_teleportGhost)
        {
            _castDeley = 0f;
            return;
        }

        if (_ghostPrepearCount <= 1) _castDeley = _baseCastDelay;
        else _castDeley = _baseCastDelay * Mathf.Pow(2, _ghostPrepearCount - 1);
    }

    private void TeleportToGhost(Character ghost)
    {
        if (ghost == null || !(ghost is MinionComponent)) return;

        CmdAcTeleportToGhost();
        ActivateWayIndicator();

        if (ghost.TryGetComponent<GhostAura>(out GhostAura ghostAura)) PerformTeleport(ghost.transform.position);
        if (manaTeleportToGhost() || !_movingToGhostWithZeroMana) RemoveGhost(ghost);
    }
    
    protected override void CommitUse()
    {
        if (_teleportGhost)
        {
            return;
        }
        
        base.CommitUse();
    }

    private void ActivateWayIndicator() => way.SetActive(true);

    private void PerformTeleport(Vector3 targetPosition)
    {
        targetPosition.y = 0f;
        var moveComponent = GetComponent<MoveComponent>();
        moveComponent?.TeleportToPositionSmooth(targetPosition, 0.5f);

        if (_isPullingHealthGostTeleport) Teleported?.Invoke(Hero, targetPosition);

        if (_teleportAnimationCoroutine != null) StopCoroutine(_teleportAnimationCoroutine);
        _teleportAnimationCoroutine = StartCoroutine(PlayTeleportMoveAnimation(targetPosition));

        StartCoroutine(DisableWayAfterTeleport(moveComponent, targetPosition));
    }

    private void RemoveGhost(Character ghost)
    {
        if (!(ghost is MinionComponent minion)) return;

        _spawnComponent.CmdRemoveUnit(minion);
        _ghosts.Remove(ghost);
        _ghostPrepearCount = _ghosts.Count;
    }

    private IEnumerator DisableWayAfterTeleport(MoveComponent moveComponent, Vector3 targetPosition)
    {
        const float maxWaitTime = 1.5f;
        float elapsedTime = 0f;

        while (elapsedTime < maxWaitTime)
        {
            if (Vector3.Distance(moveComponent.transform.position, targetPosition) < 0.2f)
                break;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        way.SetActive(false);
    }

    private void SpawnGhost(Vector3 position, Quaternion LookRotation)
    {
        if (_spawnComponent == null) return;
        RemoveOldestGhostIfNeeded();
        if (_ghosts.Count >= maxGhosts) return;
        Vector3 spawnPosition = position + Vector3.up * 1f;
        _spawnComponent.CmdSpawnAliesPoint(spawnPosition, LookRotation, null,  0, false, Hero);
    }

    private void RemoveOldestGhostIfNeeded()
    {
        if (_ghosts.Count >= maxGhosts)
        {
            var oldestGhost = _ghosts.FirstOrDefault();
            if (oldestGhost != null)
            {
                _ghosts.Remove(oldestGhost);
                _spawnComponent.CmdRemoveUnit(oldestGhost);
            }
        }
    }

    private void OnGhostSpawned(Character ghost)
    {
        if (ghost == null || _ghosts.Contains(ghost) || !(ghost is MinionComponent)) return;
        _ghosts.Add(ghost);

        if (ghost.TryGetComponent<GhostAura>(out var ghostAura))
        {
            if (_effectsInnerDarknessTalent) ghostAura.EffectsInnerDarknessTalent = true;
            if (_passingThroughGhost) ghostAura.PassingThroughGhost = true;
        }
    }

    private bool IsMouseOverGhost(out Character ghost)
    {
        ghost = null;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _targetsLayers))
        {
            ghost = _ghosts.FirstOrDefault(unit => unit != null && unit.gameObject == hit.collider.gameObject);
            return ghost != null;
        }

        return false;
    }

    private bool IsWithinRadius(Vector3 targetPosition, float radius) => Vector3.Distance(transform.position, targetPosition) <= radius;

    private bool IsVisibleToHero(Vector3 point)
    {
        Vector3 direction = point - transform.position;
        if (Physics.Raycast(transform.position + Vector3.up, direction.normalized, out var hit, direction.magnitude)) if (hit.collider.GetComponent<Character>() == Hero) return true;
        return false;
    }

    private bool TryConsumeMana(float amount)
    {
        if (_manaResource != null && _manaResource.CurrentValue >= amount)
        {
            _manaResource.CmdUse(amount);
            return true;
        }
        return false;
    }

    private IEnumerator SpawnGhostVisualEffect(Vector3 targetPosition)
    {
        _isSpawningGhostVisual = true;
        targetPosition.y = 1f;

        RemoveOldestGhostIfNeeded();
        CmdAcSummoningGhost();

        Vector3 spawnDirection = (targetPosition - transform.position).normalized;
        float offsetDistance = AreaInfo.Radius - 1;
        Vector3 spawnStartPosition = targetPosition - spawnDirection * offsetDistance;

        var ghostVisual = Instantiate(ghostPrefabPreview, spawnStartPosition, Quaternion.identity);

        if (ghostVisual.TryGetComponent<Collider>(out var collider)) collider.enabled = false;
        yield return MoveGhostVFXToPoint(ghostVisual.transform, targetPosition);
        Destroy(ghostVisual.gameObject);

        SpawnGhost(targetPosition, ghostVisual.transform.rotation);

        if (_pendingSpawn.Count > 0 && Charges.HasCharges && (isSkillEnableBoostLogic || _chargesHaveSeparateCooldown || !Cooldown.IsActive)) 
            StartCoroutine(SpawnGhostVisualEffect(_pendingSpawn.Dequeue()));
        else 
            _isSpawningGhostVisual = false;
    }

    private IEnumerator MoveGhostVFXToPoint(Transform vfx, Vector3 target)
    {
        const float moveDuration = 1f;
        float time = 0;
        Vector3 start = vfx.position;
        Vector3 end = target + Vector3.up;

        while (time < moveDuration)
        {
            time += Time.deltaTime;
            vfx.position = Vector3.Lerp(start, end, time / moveDuration);
            yield return null;
        }
    }

    private IEnumerator PlayTeleportMoveAnimation(Vector3 targetPosition)
    {
        var moveComponent = GetComponent<MoveComponent>();
        if (moveComponent == null) yield break;

        Vector3 lastPosition = transform.position;

        while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
        {
            Vector3 currentPosition = transform.position;
            Vector3 fakeVelocity = (currentPosition - lastPosition) / Time.deltaTime;
            lastPosition = currentPosition;

            moveComponent.SetAnimationMovement(fakeVelocity);

            yield return null;
        }

        moveComponent.SetAnimationMovement(Vector3.zero);
        _teleportAnimationCoroutine = null;
    }

    private IEnumerator CheckExtendedRadiusJob()
    {
        float lastCalculatedRadius = -1f;

        while (true)
        {
            float currentTargetRadius = AreaInfo.Radius + extendedRadius;

            bool ghostWithAuraInExtendedRadius = _ghosts.Any(ghost =>
                ghost != null &&
                ghost.GetComponent<GhostAura>() != null &&
                IsWithinRadius(ghost.transform.position, currentTargetRadius));

            if (_extendedRadiusCircle != null)
            {
                var color = ghostWithAuraInExtendedRadius ? Color.green : extendedRadiusColor;
                _extendedRadiusCircle.SetColor(color);

                if (!Mathf.Approximately(lastCalculatedRadius, currentTargetRadius))
                {
                    lastCalculatedRadius = currentTargetRadius;
                    
                    _extendedRadiusCircle.Clear(); 
                    _extendedRadiusCircle.Draw(currentTargetRadius);
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    protected override IEnumerator CastJob()
    {
        if (_teleportGhost && _ghostToTeleport != null)
        {
            TeleportToGhost(_ghostToTeleport);
        }
        else if (!float.IsPositiveInfinity(_spawnPosition.x) && TryConsumeMana(12))
        {
            Vector3 spawnPosition = _spawnPosition;
            StartCoroutine(SpawnGhostVisualEffect(spawnPosition));
        }
        else if (_ghostMoveToTarget && _ghostToMove != null && _targetCharacter != null)
        {
            StartCoroutine(MoveGhostToCharacter(_ghostToMove, _targetCharacter));
        }

        yield return null;
    }

    protected override void ClearData()
    {
        base.ClearData(); 

        _castDeley = _baseCastDelay;
        _targetCharacter = null;
        _spawnPosition = Vector3.positiveInfinity;
        _ghostToTeleport = null;
        _ghostToMove = null;
        _teleportGhost = false;
        _ghostMoveToTarget = false;

        _pendingSpawn.Clear();
        _isSpawningGhostVisual = false;

        if (_ghostPrefabPreview != null)
        {
            Destroy(_ghostPrefabPreview);
            _ghostPrefabPreview = null;
        }
    }

    private bool manaTeleportToGhost()
    {
        return _manaResource.CurrentValue > _manaResource.MaxValue * ManaPercentToCheckTeleport;
    }

    [Command] private void CmdAcSummoningGhost() => RpcAcSummoningGhost();
    [Command] private void CmdAcTeleportToGhost() => RpcAcTeleportToGhost();
    [Command] private void CmdAcСontrolGhostToTarget() => RpcAcСontrolGhostToTarget();

    [ClientRpc]
    private void RpcAcSummoningGhost()
    {
        if (_audioSource != null && aCSummoningGhost != null) _audioSource.PlayOneShot(aCSummoningGhost);
    }

    [ClientRpc]
    private void RpcAcTeleportToGhost()
    {
        if (_audioSource != null && aCTeleportToGhost != null) _audioSource.PlayOneShot(aCTeleportToGhost);
    }

    [ClientRpc]
    private void RpcAcСontrolGhostToTarget()
    {
        if (_audioSource != null && aCСontrolGhostToTarget != null) _audioSource.PlayOneShot(aCСontrolGhostToTarget);
    }
}