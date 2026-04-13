 using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PullingHealth : Skill
{
    [Header("Pulling Health Settings")]
    [SerializeField] private GameObject _pullingHealthPrefab;
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private Health _health;
    [SerializeField] private List<GameObject> _ghost = new List<GameObject>();
    [SerializeField] private float _tickInterval;
    [SerializeField] private AudioClip _audioClip;
    [SerializeField] private Ghost _ghostSkill;

    private GameObject _cachedTarget;
    private AudioSource _audioSource;
    private GameObject _activeEffect;
    private List<GameObject> _activeGhostEffects = new List<GameObject>();
    private List<GameObject> _allActiveEffects = new List<GameObject>();
    private float _baseRadius;
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
    private const float GhostChainRangeStep1 = 3f;
    private const float GhostChainRangeStep2 = 6f;
    private const float MaxGhostRadiusIncrease = 4f;
    private const float MaxPositionShift = 1f;
    private const float MaxGhost = 2f;

    private const float PullingHealthExitCrossFadeDuration = 0.1f;
    private const int GhostsToAddAtFirstThreshold = 1;
    private const int GhostsToAddAtSecondThreshold = 2;
    private const int GhostsToAddDefault = 0;
    private const float RadiusIncreasePerGhost = 2f;
    private const float SearchTargetRadius = 1f;

    private const string PullingHealthCastDelay = "PullingHealthCastDelay";
    private const string PullingHealthMidTrigger = "PullingHealthMidTrigger";
    private const string PullingHealthCastDelayExit = "PullingHealthCastDelayExit";
    #endregion

    private int _pullingHealthMidTriggerHash = Animator.StringToHash(PullingHealthMidTrigger);
    private int _pullingHealthCastDelayHash = Animator.StringToHash(PullingHealthCastDelay);

    private readonly List<IDamageable> _extraTargets = new();
    private readonly List<GameObject> _extraEffects = new();

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
            if (Targeting.GetTarget() != null) return Vector3.Distance(Targeting.GetTarget().Transform.position, transform.position) <= AreaInfo.Radius;
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

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _baseRadius = AreaInfo.Radius;
        _baseCastStreamDuration = CastStreamDuration;
        _baseTickInterval = _tickInterval;
    }

    private void OnDisable()
    {
        _ghostSkill.Teleported -= OnGhostTeleport;
        OnSkillCanceled -= HandleSkillCanceled;
    }

    private void OnEnable()
    {
        OnSkillCanceled += HandleSkillCanceled;
        _ghostSkill.Teleported += OnGhostTeleport;
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

        if (_pullingHealthThroughGhosts) UpdateRadiusBasedOnGhosts();
    }
    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (Targeting.GetTempTarget()?.Targetable == null)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget(Targeting.GetMousePoint(), SearchTargetRadius);

                if (Targeting.GetTempTarget()?.Targetable != null && Targeting.GetTempTarget()?.Targetable is IDamageable damageable)
                {
                    if (IsAllyTarget(damageable) || damageable as Character == Hero) Targeting.ClearTempTarget();

                    else
                    {
                        if (Targeting.GetTempTarget()?.Targetable is Character character && character.SelectedCircle != null)
                        {
                            character.SelectedCircle.IsActive = false;
                            var multiMagic = Hero.CharacterState.GetState(States.MultiMagic) as MultiMagic;
                            if (multiMagic != null) multiMagic.LastTarget = character;
                        }

                        if (_pullingHealthThroughGhosts) UpdateRadiusBasedOnGhosts();

                        break;
                    }
                }
            }
            yield return null;
        }

        Targeting.SetTarget(Targeting.GetTempTarget()?.Targetable);

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Targeting.GetTarget()?.Targetable);
        callbackDataSaved(targetInfo);
    }

    private void UpdateRadiusBasedOnGhosts()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, AreaInfo.Radius);
        int ghostCount = 0;
        foreach (var collider in hitColliders) if (collider.TryGetComponent<GhostAura>(out var ghostAura)) ghostCount++;
        AreaInfo.Radius = _baseRadius + ghostCount * RadiusIncreasePerGhost;
        AreaInfo.Radius = Mathf.Clamp(AreaInfo.Radius, _baseRadius, _baseRadius + MaxGhostRadiusIncrease);
        if (_skillRender != null) _skillRender.DrawRadius(AreaInfo.Radius);
    }

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

        var multiMagic = Hero.CharacterState.GetState(States.MultiMagic) as MultiMagic;
        if (multiMagic != null)
        {
            foreach (var characterTarget in multiMagic.PopPendingTargets())
            {
                if (characterTarget == Targeting.GetTarget()?.Character)
                {
                    _extraTargets.Add(characterTarget);

                    TryPayCost();
                    CmdSpawnExtraPullingEffect(gameObject, characterTarget.gameObject);
                }
            }
        }

        AfterCastJob();
        StartCoroutine(StreamDuration());
        while (!_streamFinished)  yield return null;
    }


    private IEnumerator StreamDuration()
    {
        IDamageable damageable = Targeting.GetTarget()?.Damageable;

        if (damageable != null) _cachedTarget = damageable.gameObject;
        else yield break;

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

        Vector3 initialPosition = transform.position;

        PlayShotSound();

        #region Pulling through Ghosts (Length)
        if (_pullingHealthThroughGhosts)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, AreaInfo.Radius);
            List<GameObject> pullingZone = new List<GameObject>();

            foreach (var collider in hitColliders)
            {
                if (collider.TryGetComponent<GhostAura>(out var ghostAura)) pullingZone.Add(ghostAura.gameObject);
                else if (collider.TryGetComponent<GrowTreeAura>(out var growTreeAura)) pullingZone.Add(growTreeAura.gameObject);
            }

            pullingZone.Sort((a, b) => Vector3.Distance(transform.position, a.transform.position).CompareTo(Vector3.Distance(transform.position, b.transform.position)));

            float targetDistance = Vector3.Distance(transform.position, damageable.transform.position);
            if (targetDistance <= _baseRadius) CmdSpawnPullingHealthEffect(gameObject, damageable.gameObject);

            if (targetDistance <= _baseRadius + GhostChainRangeStep1 && pullingZone.Count == 1)
            {
                GameObject nearestGhost = pullingZone[0];
                CmdSpawnPullingHealthEffect(gameObject, nearestGhost);
                CmdSpawnPullingHealthEffect(nearestGhost, damageable.transform.gameObject);
            }

            else if (targetDistance <= _baseRadius + GhostChainRangeStep2 && pullingZone.Count == MaxGhost)
            {
                GameObject ghost1 = pullingZone[0];
                GameObject ghost2 = pullingZone[1];

                CmdSpawnPullingHealthEffect(gameObject, ghost1);
                CmdSpawnPullingHealthEffect(ghost1, ghost2);
                CmdSpawnPullingHealthEffect(ghost2, damageable.gameObject);
            }
        }
        #endregion

        else
        {
            float targetDistance = Vector3.Distance(transform.position, damageable.transform.position);
            if (targetDistance <= _baseRadius) CmdSpawnPullingHealthEffect(gameObject, damageable.gameObject);
        }

        while (elapsed < CastStreamDuration)
        {
            var target = Targeting.GetTarget()?.Character;
            if (target == null || target.IsDead)
            {
                EndAnimDestroyEffect();
                _isStreaming = false;
                TryCancel();
                yield break;
            }

            if (_ignoreMoveCheck)
            {
                initialPosition = transform.position;
                _ignoreMoveTimeLeft -= Time.deltaTime;
                if (_ignoreMoveTimeLeft <= 0f) _ignoreMoveCheck = false;
            }

            if (damageable != null && (Input.GetMouseButtonDown(1) || ( Vector3.Distance(transform.position, damageable.transform.position) > AreaInfo.Radius)) || Vector3.Distance(initialPosition, transform.position) > MaxPositionShift && !_ignoreMoveCheck)
            {
                EndAnimDestroyEffect();
                _isStreaming = false;
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
                damageTickElapsed = 0f;
            }

            if (manaResource.CurrentValue < MinManaToStream)
            {
                CmdDestroyEffect();
                _isStreaming = false;
                yield break;
            }

            elapsed += Time.deltaTime;
            damageTickElapsed += Time.deltaTime;
            yield return null;
        }

        FinishStream();
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
        StopCoroutine(StreamDuration());
        AfterCastJob();
        StopShotSound();
    }
    [Command] private void CmdSyncGhosts(GameObject ghostObj) => _ghost.Add(ghostObj);

    [Command]
    private void CmdSpawnPullingHealthEffect(GameObject startPoint, GameObject targetPoint)
    {
        if (_pullingHealthPrefab == null || startPoint == null || targetPoint == null) return;

        GameObject effectInstance = Instantiate(_pullingHealthPrefab, startPoint.transform.position, Quaternion.identity);
        //SceneManager.MoveGameObjectToScene(effectInstance, _hero.NetworkSettings.MyRoom);
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
            //SceneManager.MoveGameObjectToScene(ghostEffectInstance, _hero.NetworkSettings.MyRoom);
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
        //SceneManager.MoveGameObjectToScene(effect, _hero.NetworkSettings.MyRoom);
        NetworkServer.Spawn(effect);
        RpcInitEffects(effect, start, target);
    }

    [Command]
    private void CmdDestroyEffect()
    {
        if (_activeEffect != null)
        {
            Debug.Log($"Destroying active effect: {_activeEffect.name}");
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
                Debug.Log($"Destroying additional effect: {_allActiveEffects[i].name}");
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
            Debug.Log($"Destroying effect on client: {effect.name}");
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
        Targeting.ClearTempTarget();
        Targeting.ClearTarget();
        AreaInfo.Radius = _baseRadius;
    }
}
