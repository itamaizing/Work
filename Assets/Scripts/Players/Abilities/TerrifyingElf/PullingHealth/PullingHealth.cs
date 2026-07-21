using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PullingHealth : Skill, IMultiMagicSkill
{
    [Header("Pulling Health Settings")]
    [SerializeField] private GameObject _pullingHealthPrefab;
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private Health _health;
    [SerializeField] private List<GameObject> _ghost = new List<GameObject>();
    [SerializeField] private float _tickInterval;
    [SerializeField] private AudioClip _audioClip;
    [SerializeField] private Ghost _ghostSkill;
    [SerializeField] private float _radiusIncreasePerGhost = 3f;
    
    private float ChainNodeRadius => AreaInfo.Radius + _radiusIncreasePerGhost;

    private GameObject _cachedTarget;
    private AudioSource _audioSource;
    private GameObject _activeEffect;
    private List<GameObject> _activeGhostEffects = new List<GameObject>();
    private List<GameObject> _allActiveEffects = new List<GameObject>();
    private float _baseTickInterval;
    private float _baseCastStreamDuration;
    private float _ignoreMoveTimeLeft;
    private bool _ignoreMoveCheck;
    private bool _isStreaming;
    private bool _streamFinished;

    #region const
    private const float TeleportTime = 0.3f;
    private const float GhostDamagePercent = 0.3f;
    private const float GhostHealPercent = 0.70f;
    private const float BaseDurationMultiplier = 1.4f;
    private const float DurationPerStack = 0.1f;
    private const int InnerDarknessFirstThreshold = 2;
    private const int InnerDarknessSecondThreshold = 4;
    private const float FearTickSpeedMultiplier = 0.5f;
    private const int MinManaToStream = 2;
    private const float MaxPositionShift = 1f;

    private const float ManaCostPerTick = 2f;
    private const float PullingHealthExitCrossFadeDuration = 0.1f;
    private const int GhostsToAddAtFirstThreshold = 1;
    private const int GhostsToAddAtSecondThreshold = 2;
    private const int GhostsToAddDefault = 0;
    private const float SearchTargetRadius = 1f;

    private const string PullingHealthCastDelay = "PullingHealthCastDelay";
    private const string PullingHealthMidTrigger = "PullingHealthMidTrigger";
    private const string PullingHealthCastDelayExit = "PullingHealthCastDelayExit";
    #endregion

    private int _pullingHealthMidTriggerHash = Animator.StringToHash(PullingHealthMidTrigger);
    private int _pullingHealthCastDelayHash = Animator.StringToHash(PullingHealthCastDelay);

    private readonly List<IDamageable> _extraTargets = new();
    private readonly List<GameObject> _extraEffects = new();
    
    private Coroutine _streamCoroutine;
    private float _streamAccumulatedRollback = 0f;
    protected override bool IsCustomStreamActive => _isStreaming;
    protected override bool SkipLegacyCastStreamJob => true;

    #region Talent
    private bool _pullingHealthThroughGhosts;
    private bool pullingHealthGhostTalent;
    private bool _pullingHealthSpeedWithFearTalent;
    #endregion

    protected override int AnimTriggerCastDelay => _pullingHealthCastDelayHash;
    protected override int AnimTriggerCast => 0;

    protected override bool IsCanCast
    {
        get
        {
            if (_isStreaming) return false;
            var target = Targeting.GetTarget();
            if (target != null)
            {
                if (_pullingHealthThroughGhosts)
                {
                    BuildChain();
                    return IsPositionReachable(target.Transform.position);
                }
                return Vector3.Distance(target.Transform.position, transform.position) <= AreaInfo.Radius;
            }
            return false;
        }
    }

    private bool IsAllyTarget(IDamageable target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");

    public event Action<Transform, IDamageable, int> OnInnerDarknessTriggered;

    public void MovePullingHealth()
    {
        _hero.Move.StopMoveAndAnimationMove();
        _hero.Move.IsMoveBlocked = true;
    }

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);
        _audioSource = GetComponent<AudioSource>();
        _baseCastStreamDuration = CastStreamDuration;
        _baseTickInterval = _tickInterval;
    }

    private void OnDisable()
    {
        _ghostSkill.Teleported -= OnGhostTeleport;
        OnSkillCanceled -= HandleSkillCanceled;
        CastStreamRolledBack -= OnStreamRollbackReceived;
    }

    private void OnEnable()
    {
        OnSkillCanceled += HandleSkillCanceled;
        _ghostSkill.Teleported += OnGhostTeleport;
        CastStreamRolledBack += OnStreamRollbackReceived;
    }
    
    protected override void PlayPrepareAnim()
    {
        Animation.PlayTrigger(PullingHealthCastDelay);
    }
    
    private void OnStreamRollbackReceived(float amount)
    {
        _streamAccumulatedRollback += amount;
    }

    private void OnGhostTeleport(Character character, Vector3 _)
    {
        if (character == Hero)
        {
            _ignoreMoveCheck = true;
            _ignoreMoveTimeLeft = TeleportTime;
        }
    }
    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0) Targeting.SetTarget(targetInfo.GetTargets()[0]);
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (true)
        {
            Vector3 mousePoint = Targeting.GetMousePoint();

            if (_pullingHealthThroughGhosts)
            {
                Collider[] hoverHits = Physics.OverlapSphere(mousePoint, SearchTargetRadius);
                Character hoveredCharacter = null;
                foreach (var h in hoverHits)
                {
                    if (h.TryGetComponent<Character>(out var c) && c != Hero && !IsAllyTarget(c))
                    {
                        hoveredCharacter = c;
                        break;
                    }
                }
                
                if (hoveredCharacter != null)
                    UpdateChainVisuals(hoveredCharacter.transform.position);
                else
                    UpdateChainVisuals(null); 
            }

            if (GetMouseButton)
            {
                Targeting.FindTempTarget(mousePoint, SearchTargetRadius);
                if (Targeting.GetTempTarget()?.Targetable != null
                    && Targeting.GetTempTarget()?.Targetable is IDamageable damageable)
                {
                    if (IsAllyTarget(damageable) || damageable as Character == Hero)
                        Targeting.ClearTempTarget();
                    else
                    {
                        if (Targeting.GetTempTarget()?.Targetable is Character character
                            && character.SelectedCircle != null)
                        {
                            character.SelectedCircle.IsActive = false;
                            var multiMagic = Hero.CharacterState.GetState(States.MultiMagic) as MultiMagic;
                            if (multiMagic != null) multiMagic.LastTarget = character;
                        }

                        ClearChainVisuals(); 
                        break;
                    }
                }
            }

            yield return null;
        }

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Targeting.GetTempTarget()?.Targetable);
        callbackDataSaved(targetInfo);
    }

    #region ChainSystem

    [SerializeField] private DrawCircle _chainUnitRadiusPrefab;
    private readonly List<DrawCircle> _activeChainCircles = new();
    private readonly List<GameObject> _currentChain = new();
    private readonly Dictionary<GameObject, GameObject> _chainParents = new();

    private void BuildChain()
    {
        _currentChain.Clear();
        _chainParents.Clear();

        var visited = new HashSet<GameObject>();
        var queue = new Queue<GameObject>();

        var initialHits = Physics.OverlapSphere(transform.position, AreaInfo.Radius);
        foreach (var hit in initialHits)
        {
            if (!IsChainUnit(hit)) continue;
            if (visited.Add(hit.gameObject))
            {
                _currentChain.Add(hit.gameObject);
                queue.Enqueue(hit.gameObject);
                _chainParents[hit.gameObject] = gameObject;
            }
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var hits = Physics.OverlapSphere(current.transform.position, ChainNodeRadius);
            foreach (var hit in hits)
            {
                if (!IsChainUnit(hit)) continue;
                if (visited.Add(hit.gameObject))
                {
                    _currentChain.Add(hit.gameObject);
                    queue.Enqueue(hit.gameObject);
                    _chainParents[hit.gameObject] = current;
                }
            }
        }
    }

    private bool IsPositionReachable(Vector3 targetPosition)
    {
        if (Vector3.Distance(transform.position, targetPosition) <= AreaInfo.Radius)
            return true;

        foreach (var unit in _currentChain)
        {
            if (Vector3.Distance(unit.transform.position, targetPosition) <= ChainNodeRadius)
                return true;
        }

        return false;
    }
    
    private bool IsAllied(GameObject obj)
    {
        if (obj == null) return false;
        
        if (obj.TryGetComponent<Object>(out var targetObj))
        {
            return targetObj.IndexTeam == Hero.NetworkSettings.TeamIndex;
        }

        if (obj.TryGetComponent<Character>(out var minion))
        {
            return minion.NetworkSettings.TeamIndex == Hero.NetworkSettings.TeamIndex;
        }

        return false;
    }

    private bool IsChainUnit(Collider collider)
    {
        var ghost = collider.GetComponentInParent<GhostAura>();
        var tree = collider.GetComponentInParent<GrowTreeAura>();

        if (ghost == null && tree == null) return false;

        GameObject rootObj = ghost != null ? ghost.gameObject : tree.gameObject;

        return IsAllied(rootObj);
    }

    private void UpdateChainVisuals(Vector3? targetPosition)
    {
        BuildChain();

        foreach (var circle in _activeChainCircles)
            if (circle != null)
                Destroy(circle.gameObject);
        _activeChainCircles.Clear();

        Color circleColor = Color.green;
        if (targetPosition.HasValue)
        {
            bool canReach = IsPositionReachable(targetPosition.Value);
            circleColor = canReach ? Color.green : Color.yellow;
        }

        foreach (var unit in _currentChain)
        {
            if (_chainUnitRadiusPrefab == null) break;
            var circle = Instantiate(_chainUnitRadiusPrefab, unit.transform);
            circle.SetColor(circleColor);
            circle.Draw(ChainNodeRadius);
            _activeChainCircles.Add(circle);
        }
    }

    private void ClearChainVisuals()
    {
        foreach (var circle in _activeChainCircles)
            if (circle != null)
                Destroy(circle.gameObject);
        _activeChainCircles.Clear();
    }

    private List<GameObject> BuildEffectivePath(GameObject target)
    {
        var path = new List<GameObject>();
        Vector3 targetPos = target.transform.position;

        if (Vector3.Distance(transform.position, targetPos) <= AreaInfo.Radius)
        {
            path.Add(gameObject);
            path.Add(target);
            return path;
        }

        GameObject reachingNode = null;
        float minDistanceToTarget = float.MaxValue;

        foreach (var unit in _currentChain)
        {
            float dist = Vector3.Distance(unit.transform.position, targetPos);
            if (dist <= ChainNodeRadius)
            {
                if (dist < minDistanceToTarget)
                {
                    minDistanceToTarget = dist;
                    reachingNode = unit;
                }
            }
        }

        if (reachingNode != null)
        {
            List<GameObject> tempPath = new List<GameObject> { target };
            GameObject current = reachingNode;

            while (current != gameObject && current != null)
            {
                tempPath.Add(current);
                if (_chainParents.TryGetValue(current, out GameObject parent))
                {
                    current = parent;
                }
                else
                {
                    break;
                }
            }

            if (!tempPath.Contains(gameObject))
            {
                tempPath.Add(gameObject);
            }

            tempPath.Reverse();
            return tempPath;
        }

        path.Add(gameObject);
        path.Add(target);
        return path;
    }

    #endregion
    
    protected override IEnumerator CastJob()
    {
        if (Targeting.GetTarget() == null) yield return null;

        _hero.Animator.SetTrigger(_pullingHealthMidTriggerHash);
        _hero.NetworkAnimator.SetTrigger(_pullingHealthMidTriggerHash);

        int innerDarknessStacks;

        #region Work with InnerDarkness
        if (Targeting.GetTarget()?.Character is Character character)
        {
            var targetComponentState = character.GetComponent<CharacterState>();

            if (pullingHealthGhostTalent && targetComponentState.CheckForState(States.InnerDarkness))
            {
                innerDarknessStacks = targetComponentState.CheckStateStacks(States.InnerDarkness);
                Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, AreaInfo.Radius);

                int ghostsToAdd = innerDarknessStacks == InnerDarknessFirstThreshold ? GhostsToAddAtFirstThreshold : innerDarknessStacks == InnerDarknessSecondThreshold ? GhostsToAddAtSecondThreshold : GhostsToAddDefault;
                int addedGhosts = 0;

                foreach (var obj in nearbyObjects)
                {
                    if (addedGhosts >= ghostsToAdd) break;

                    if (obj.TryGetComponent<GhostAura>(out GhostAura ghostAura))
                    {
                        if (!IsAllied(obj.gameObject)) continue;

                        float distanceToTarget = Vector3.Distance(obj.transform.position, character.transform.position);
                        if (distanceToTarget <= AreaInfo.Radius && !_ghost.Contains(obj.gameObject))
                        {
                            _ghost.Add(obj.gameObject);
                            CmdSyncGhosts(obj.gameObject);
                            addedGhosts++;
                        }
                    }
                }

                CmdSpawnPullingHealthEffectGhost(character.gameObject);

                if (innerDarknessStacks > InnerDarknessFirstThreshold)
                {
                    float durationMultiplier = BaseDurationMultiplier + DurationPerStack * (innerDarknessStacks - 1);
                    Channeling.CastDuration = _baseCastStreamDuration * durationMultiplier;
                }
            }

            if (_pullingHealthSpeedWithFearTalent && targetComponentState.CheckForState(States.Fear))
            {
                _tickInterval *= FearTickSpeedMultiplier;
            }
        }
        #endregion

        AfterCastJob();
        _streamCoroutine = StartCoroutine(StreamDuration());
        while (!_streamFinished)  yield return null;
    }
    
    public void HandleExtraTarget(Character target)
    {
        _extraTargets.Add(target);
        TryPayCost();
        CmdSpawnExtraPullingEffect(gameObject, target.gameObject);
    }

    private IEnumerator StreamDuration()
    {
        IDamageable damageable = Targeting.GetTarget()?.Damageable;

        if (damageable != null) _cachedTarget = damageable.gameObject;
        else yield break;

        _streamAccumulatedRollback = 0f;
        _isStreaming = true;
        _streamFinished = false;
        
        float elapsed = 0f;
        float damageTickElapsed = 0f;
        var manaResource = Hero.TryGetResource(ResourceType.Mana);

        if (manaResource == null || manaResource.CurrentValue < MinManaToStream)
        {
            CmdDestroyEffect();
            _isStreaming = false;
            yield break;
        }
        
        InvokeCastStreamStarted(CastStreamDuration);
        Vector3 initialPosition = transform.position;
        PlayShotSound();

        #region Pulling through Ghosts (Length)
        if (_pullingHealthThroughGhosts)
        {
            BuildChain();

            if (_currentChain.Count == 0)
            {
                if (Vector3.Distance(transform.position, damageable.transform.position) <= AreaInfo.Radius)
                    CmdSpawnPullingHealthEffect(gameObject, damageable.gameObject);
            }
            else
            {
                var effectivePath = BuildEffectivePath(damageable.gameObject);
                for (int i = 0; i < effectivePath.Count - 1; i++)
                    CmdSpawnPullingHealthEffect(effectivePath[i], effectivePath[i + 1]);
            }
        }
        else
        {
            float targetDistance = Vector3.Distance(transform.position, damageable.transform.position);
            if (targetDistance <= AreaInfo.Radius) CmdSpawnPullingHealthEffect(gameObject, damageable.gameObject);
        }
        #endregion

        while (elapsed < CastStreamDuration)
        {
            if (_streamAccumulatedRollback > 0f)
            {
                float consumed = _streamAccumulatedRollback;
                elapsed += consumed;
                _streamAccumulatedRollback = 0f;
                RaiseCastStreamProgressApplied(consumed);
            }
            
            var target = Targeting.GetTarget()?.Character;
            if (target == null || target.IsDead)
            {
                EndAnimDestroyEffect();
                _isStreaming = false;
                _streamCoroutine = null;
                TryCancel();
                yield break;
            }

            if (_ignoreMoveCheck)
            {
                initialPosition = transform.position;
                _ignoreMoveTimeLeft -= Time.deltaTime;
                if (_ignoreMoveTimeLeft <= 0f) _ignoreMoveCheck = false;
            }

            bool isTargetOutOfRange = false;
            if (_pullingHealthThroughGhosts)
            {
                BuildChain();
                isTargetOutOfRange = !IsPositionReachable(damageable.transform.position);
            }
            else
            {
                isTargetOutOfRange = Vector3.Distance(transform.position, damageable.transform.position) > AreaInfo.Radius;
            }

            if (damageable != null && (Input.GetMouseButtonDown(1) || isTargetOutOfRange || Vector3.Distance(initialPosition, transform.position) > MaxPositionShift && !_ignoreMoveCheck))
            {
                EndAnimDestroyEffect();
                _isStreaming = false;
                _streamCoroutine = null;
                TryCancel();
                yield break;
            }

            if (damageable != null)
            {
                Vector3 directionToTarget = (damageable.transform.position - transform.position).normalized;
                directionToTarget.y = 0;
                transform.rotation = Quaternion.LookRotation(directionToTarget);
            }

            if (damageTickElapsed >= _tickInterval)
            {
                ApplyDamageToTarget();
                HealPlayer();
                foreach (var ghost in _ghost) ApplyDamageThroughGhost(ghost);
                manaResource.CmdUse(ManaCostPerTick);
                damageTickElapsed = 0f;
            }

            if (manaResource.CurrentValue < MinManaToStream)
            {
                CmdDestroyEffect();
                _isStreaming = false;
                _streamCoroutine = null;
                yield break;
            }

            elapsed += Time.deltaTime;
            damageTickElapsed += Time.deltaTime;
            yield return null;
        }

        FinishStream();
        _streamCoroutine = null;
    }

    private void FinishStream()
    {
        Channeling.CastDuration = _baseCastStreamDuration;
        _tickInterval = _baseTickInterval;
        _isStreaming = false;
        _streamFinished = true;

        Hero.Move.IsMoveBlocked = false;
        Hero.Move.StopLookAt();
        Hero.Animator.speed = 1;

        CmdDestroyEffect();
    }

    private void EndAnimDestroyEffect()
    {
        _hero.Animator.ResetTrigger(_pullingHealthMidTriggerHash);
        _hero.NetworkAnimator.ResetTrigger(_pullingHealthMidTriggerHash);

        CmdCrossFade();
        _hero.Animator.CrossFade(PullingHealthCastDelayExit, PullingHealthExitCrossFadeDuration);

        Hero.Move.IsMoveBlocked = false;
        Hero.Move.StopLookAt();
        Hero.Animator.speed = 1;
        CmdDestroyEffect();
    }

    private void ApplyDamageThroughGhost(GameObject ghost)
    {
        if (ghost.TryGetComponent<Health>(out Health ghostHealth))
        {
            float ghostBaseDamage = Damage * GhostDamagePercent;

            Damage damage = new Damage
            {
                Value = ghostBaseDamage,
                Type = Info.DamageType,
            };

            if (_cachedTarget != null) CmdApplyDamage(damage, _cachedTarget);

            float ghostHealValue = Damage * GhostHealPercent;
            ghostHealth.CmdAdd(ghostHealValue);
        }
    }

    private void ApplyDamageToTarget()
    {
        Damage damage = new Damage
        {
            Value = Damage,
            Type = Info.DamageType,
        };

        if (_cachedTarget != null) CmdApplyDamage(damage, _cachedTarget);
        foreach (var damageble in _extraTargets) CmdApplyDamage(damage, damageble.gameObject);
    }
    private void HealPlayer()
    {
        if (_health == null) return;

        Heal heal = new Heal
        {
            Value = Damage,
        };

        _health.CmdAdd(heal.Value);
    }

    #region Talents
    public void SetPullingHealthGhostTalentActive(bool value) => pullingHealthGhostTalent = value;
    public void PullingHealthSpeedWithFearTalentActive(bool value) => _pullingHealthSpeedWithFearTalent = value;
    public void PullingHealthThroughGhosts(bool value) => _pullingHealthThroughGhosts = value;
    #endregion

    private void HandleSkillCanceled()
    {
        if (_hero != null && _hero.Move != null)
        {
            Hero.Move.IsMoveBlocked = false;
            Hero.Move.StopLookAt();
            Hero.Animator.speed = 1;
        }

        Targeting.ClearTarget();
        Targeting.ClearTempTarget();
        _extraTargets.Clear();
        _extraEffects.Clear();
        ClearChainVisuals();
        _currentChain.Clear();

        if (_streamCoroutine != null)
        {
            StopCoroutine(_streamCoroutine);
            _streamCoroutine = null;
        }
        _isStreaming = false;
        _streamAccumulatedRollback = 0f;

        AfterCastJob();
        StopShotSound();
    }
    
    [Command] private void CmdSyncGhosts(GameObject ghostObj) => _ghost.Add(ghostObj);

    [Command]
    private void CmdSpawnPullingHealthEffect(GameObject startPoint, GameObject targetPoint)
    {
        if (_pullingHealthPrefab == null || startPoint == null || targetPoint == null) return;

        GameObject effectInstance = Instantiate(_pullingHealthPrefab, startPoint.transform.position, Quaternion.identity);
        NetworkServer.Spawn(effectInstance);
        RpcInitEffects(effectInstance, startPoint, targetPoint);

        _allActiveEffects.Add(effectInstance);
        _activeEffect = effectInstance;
    }

    [Command]
    private void CmdSpawnPullingHealthEffectGhost(GameObject targetPoint)
    {
        if (_pullingHealthPrefab == null || targetPoint == null) return;

        for (int i = 0; i < _ghost.Count; i++)
        {
            GameObject ghostEffectInstance = Instantiate(_pullingHealthPrefab, _ghost[i].transform.position, Quaternion.identity);
            _activeGhostEffects.Add(ghostEffectInstance);
            NetworkServer.Spawn(ghostEffectInstance);
            RpcInitEffects(ghostEffectInstance, _ghost[i], targetPoint);
        }
    }

    [Command]
    private void CmdSpawnExtraPullingEffect(GameObject start, GameObject target)
    {
        if (_pullingHealthPrefab == null || start == null || target == null) return;

        var effect = Instantiate(_pullingHealthPrefab, start.transform.position, Quaternion.identity);
        _extraEffects.Add(effect);
        NetworkServer.Spawn(effect);
        RpcInitEffects(effect, start, target);
    }

    [Command]
    private void CmdDestroyEffect()
    {
        if (_activeEffect != null)
        {
            NetworkServer.Destroy(_activeEffect);
            RpcDestroyClientEffect(_activeEffect);
            _activeEffect = null;
        }

        for (int i = 0; i < _activeGhostEffects.Count; i++)
        {
            if (_activeGhostEffects.Count > 0)
            {
                NetworkServer.Destroy(_activeGhostEffects[i]);
                RpcDestroyClientEffect(_activeGhostEffects[i]);
            }
        }

        foreach (var effect in _extraEffects) if (effect != null) NetworkServer.Destroy(effect);

        _activeGhostEffects.Clear();
        _ghost.Clear();

        for (int i = 0; i < _allActiveEffects.Count; i++)
        {
            if (_allActiveEffects[i] != null)
            {
                NetworkServer.Destroy(_allActiveEffects[i]);
                RpcDestroyClientEffect(_allActiveEffects[i]);
            }
        }
        _allActiveEffects.Clear();
    }
    [Command] private void CmdCrossFade() => _hero.Animator.CrossFade(PullingHealthCastDelayExit, 0.1f);

    [ClientRpc]
    private void RpcInitEffects(GameObject effectGameObject, GameObject startPoint, GameObject targetPoint)
    {
        if (effectGameObject == null) return;

        PullingHealthEffect[] effects = effectGameObject.GetComponentsInChildren<PullingHealthEffect>();

        foreach (var effect in effects)
        {
            effect.Initialize(startPoint, targetPoint);
            effect.Activate();
        }
    }

    [ClientRpc]
    private void RpcDestroyClientEffect(GameObject effect)
    {
        if (effect != null)
        {
            Destroy(effect);
        }

        _activeGhostEffects.Clear();
        _ghost.Clear();
    }

    private void PlayShotSound()
    {
        if (_audioSource != null && _audioClip != null) _audioSource.PlayOneShot(_audioClip);
    }

    private void StopShotSound()
    {
        if (_audioSource != null) _audioSource.Stop();
    }
    protected override void ClearData()
    {
        _cachedTarget = null;
        _extraTargets.Clear();
        _extraEffects.Clear();
        ClearChainVisuals();
        _currentChain.Clear();
        Targeting.ClearTempTarget();
        Targeting.ClearTarget();
    }
}