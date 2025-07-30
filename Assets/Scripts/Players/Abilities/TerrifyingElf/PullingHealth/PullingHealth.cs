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
    [SerializeField] private Health health;
    [SerializeField] private List<GameObject> ghost = new List<GameObject>();
    [SerializeField] private float tickInterval;
    [SerializeField] private AudioClip audioClip;

    //[Header("Pulling Ghost")]
    //[SerializeField] private float radiusGhost;
    [SerializeField] private bool _pullingHealthThroughGhosts;
    [SerializeField] private bool pullingHealthGhostTalent;
    [SerializeField] private bool _pullingHealthSpeedWithSilenceTalent;

    private AudioSource _audioSource;
    private GameObject _activeEffect;
    private List<GameObject> _activeGhostEffects = new List<GameObject>();
    private List<GameObject> _allActiveEffects = new List<GameObject>();
    private Character _targetCharacter;
    private ObjectHealth _targetObject;
    private IDamageable _target;
    private float _baseRadius;
    private float _baseTickInterval;
    private float _baseCastStreamDuration;
    private Vector3 _targetPoint = Vector3.positiveInfinity;
    private Transform _targetTransform;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("PullingHealthCastDelay");

    protected override bool IsCanCast
    {
        get
        {
            if (_targetCharacter != null) return Vector3.Distance(_targetCharacter.transform.position, transform.position) <= Radius;
            else if (_targetObject != null) return Vector3.Distance(_targetObject.transform.position, transform.position) <= Radius;
            return false;
        }
    }

    public event Action<Transform, IDamageable, int> OnInnerDarknessTriggered;

    public void PullingHealthCast() => AnimStartCastCoroutine();
    public void PullingHealthEnd() => AnimCastEnded();

    public void MovePullingHealth()
    {
        _hero.Move.CanMove = false;
        _hero.Move.StopMoveAndAnimationMove();
    }

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _baseRadius = Radius;
        _baseCastStreamDuration = CastStreamDuration;
        _baseTickInterval = tickInterval;
    }

    private void OnDestroy()
    {
        OnSkillCanceled -= HandleSkillCanceled;
    }

    private void OnEnable()
    {
        OnSkillCanceled += HandleSkillCanceled;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (float.IsPositiveInfinity(_targetPoint.x) && !_disactive)
        {
            if (GetMouseButton)
            {
                #region Old
                //if (GetTarget().isCharater)
                //{
                //    _target = GetTarget().character;
                //    _targetPoint = _target.transform.position;
                //}
                #endregion

                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out var hitCharacter, Mathf.Infinity, _targetsLayers) 
                    && hitCharacter.collider.TryGetComponent<Character>(out Character targetCharacter))
                {
                    _targetCharacter = targetCharacter;
                    _target = _targetCharacter;
                    _targetTransform = targetCharacter.transform;
                    _targetPoint = _targetCharacter.transform.position;
                }

                else if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out var hitObject, Mathf.Infinity, _targetsLayers)
                        && hitObject.collider.TryGetComponent<Object>(out Object targetObject) && targetObject.Live)
                {
                    _targetObject = targetObject.ObjectHealth;
                    _target = _targetObject;
                    _targetTransform = targetObject.transform;
                    _targetPoint = _targetTransform.position;
                }
            }

            if (_pullingHealthThroughGhosts) UpdateRadiusBasedOnGhosts();

            yield return null;
        }

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Points.Add(_targetPoint);
        callbackDataSaved(targetInfo);
    }

    private void UpdateRadiusBasedOnGhosts()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, Radius);

        int ghostCount = 0;

        foreach (var collider in hitColliders)
        {
            if (collider.TryGetComponent<GhostAura>(out var ghostAura))
            {
                ghostCount++;
            }
        }

        Radius = _baseRadius + ghostCount * 2;

        Radius = Mathf.Clamp(Radius, _baseRadius, _baseRadius + 4);

        if (_skillRender != null)
        {
            _skillRender.DrawRadius(Radius);
        }
    }

    protected override IEnumerator CastJob()
    {
        int innerDarknessStacks;

        #region Work with InnerDarkness
        if (_targetCharacter is Component targetComponent)
        {
            var targetComponentState = targetComponent.GetComponent<CharacterState>();

            if (pullingHealthGhostTalent && targetComponentState.CheckForState(States.InnerDarkness))
            {
                innerDarknessStacks = targetComponentState.CheckStateStacks(States.InnerDarkness);
                Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, Radius);

                int ghostsToAdd = innerDarknessStacks == 2 ? 1 : innerDarknessStacks == 4 ? 2 : 0;
                int addedGhosts = 0;

                foreach (var obj in nearbyObjects)
                {
                    if (addedGhosts >= ghostsToAdd)
                        break;

                    if (obj.TryGetComponent<GhostAura>(out GhostAura ghostAura))
                    {
                        float distanceToTarget = Vector3.Distance(obj.transform.position, _targetTransform.position);
                        if (distanceToTarget <= Radius && !ghost.Contains(obj.gameObject))
                        {
                            ghost.Add(obj.gameObject);
                            CmdSyncGhosts(obj.gameObject);
                            addedGhosts++;
                        }
                    }
                }

                CmdSpawnPullingHealthEffectGhost(_targetTransform.gameObject);

                if (innerDarknessStacks > 0)
                {
                    float durationMultiplier = 1.4f + 0.1f * (innerDarknessStacks - 1);
                    CastStreamDuration = _baseCastStreamDuration * durationMultiplier;
                }
            }

            if (_pullingHealthSpeedWithSilenceTalent && targetComponentState.CheckForState(States.Silent))
            {
                float speedModifier = 0.7f;
                tickInterval *= speedModifier;
            }
        }
        #endregion

        _hero.Animator.SetTrigger(AnimTriggerCastDelay);
        _hero.NetworkAnimator.SetTrigger(AnimTriggerCastDelay);

        yield return StartCoroutine(StreamDuration());

        _hero.Animator.SetTrigger(Animator.StringToHash("PullingHealthCastDelayExit"));
        _hero.NetworkAnimator.SetTrigger(Animator.StringToHash("PullingHealthCastDelayExit"));
    }


    private IEnumerator StreamDuration()
    {
        float elapsed = 0f;
        float damageTickElapsed = 0f;
        float positionThreshold = 1f;
        var manaResource = Hero.TryGetResource(ResourceType.Mana);

        if (manaResource == null || manaResource.CurrentValue < 2)
        {
            CmdDestroyEffect();
            TryCancel();
            yield break;
        }

        Vector3 initialPosition = transform.position;

        CmdPlayShotSound();

        #region Pulling through Ghosts (Length)
        if (_pullingHealthThroughGhosts)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, Radius);
            List<GhostAura> ghostsInZone = new List<GhostAura>();

            foreach (var collider in hitColliders)
                if (collider.TryGetComponent<GhostAura>(out var ghostAura)) ghostsInZone.Add(ghostAura);

            ghostsInZone.Sort((a, b) => Vector3.Distance(transform.position, a.transform.position)
                                .CompareTo(Vector3.Distance(transform.position, b.transform.position)));

            float targetDistance = Vector3.Distance(transform.position, _targetTransform.position);
            if (targetDistance <= _baseRadius) CmdSpawnPullingHealthEffect(gameObject, _targetTransform.gameObject);

            if (targetDistance <= _baseRadius + 3 && ghostsInZone.Count == 1)
            {
                GameObject nearestGhost = ghostsInZone[0].gameObject;
                CmdSpawnPullingHealthEffect(gameObject, nearestGhost);
                CmdSpawnPullingHealthEffect(nearestGhost, _targetTransform.gameObject);
            }

            else if (targetDistance <= _baseRadius + 6 && ghostsInZone.Count == 2)
            {
                GameObject ghost1 = ghostsInZone[0].gameObject;
                GameObject ghost2 = ghostsInZone[1].gameObject;

                CmdSpawnPullingHealthEffect(gameObject, ghost1);
                CmdSpawnPullingHealthEffect(ghost1, ghost2);
                CmdSpawnPullingHealthEffect(ghost2, _targetTransform.gameObject);
            }
        }
        #endregion

        else
        {
            float targetDistance = Vector3.Distance(transform.position, _targetTransform.position);
            if (targetDistance <= _baseRadius) CmdSpawnPullingHealthEffect(gameObject, _targetTransform.gameObject);
        }

        while (elapsed < CastStreamDuration)
        {
            if ((_target as UnityEngine.Object) == null)
            {
                TryCancel();
                CmdDestroyEffect();
                yield break;
            }

            if (Input.GetMouseButtonDown(1) || (_targetCharacter != null && Vector3.Distance(transform.position, _targetTransform.position) > Radius) 
            || Vector3.Distance(initialPosition, transform.position) > positionThreshold)
            {
                _hero.Animator.ResetTrigger(Animator.StringToHash("PullingHealthCastDelay"));
                _hero.NetworkAnimator.ResetTrigger(Animator.StringToHash("PullingHealthCastDelay"));

                CmdCrossFade();
                _hero.Animator.CrossFade("PullingHealthCastDelayExit", 0.1f);

                TryCancel();
                CmdDestroyEffect();
                yield break;
            }


            if (_targetCharacter is Component targetComponent)
            {
                Vector3 directionToTarget = (targetComponent.transform.position - transform.position).normalized;
                directionToTarget.y = 0;
                transform.rotation = Quaternion.LookRotation(directionToTarget);
            }

            if (damageTickElapsed >= tickInterval)
            {
                ApplyDamageToTarget();
                HealPlayer();

                foreach (var ghost in ghost) ApplyDamageThroughGhost(ghost);
                damageTickElapsed = 0f;
            }

            if (manaResource.CurrentValue < 2)
            {
                CmdDestroyEffect();
                TryCancel();
                yield break;
            }

            elapsed += Time.deltaTime;
            damageTickElapsed += Time.deltaTime;
            yield return null;
        }

        CastStreamDuration = _baseCastStreamDuration;
        tickInterval = _baseTickInterval;
        TryCancel();

        CmdDestroyEffect();
    }

    private void ApplyDamageThroughGhost(GameObject ghost)
    {
        if (ghost.TryGetComponent<Health>(out Health ghostHealth))
        {
            //float ghostBaseDamage = Damage * 0.3f;

            Damage damage = new Damage
            {
                Value = Damage,
                Type = DamageType,
            };

            if (_targetCharacter is Component targetComponent)
            {
                //CmdApplyDamage(targetComponent.gameObject, _damage, null);
                CmdApplyDamage(damage, targetComponent.gameObject);
            }

            float healValue = Damage * 0.25f;
            health.CmdAdd(healValue);

            float ghostHealValue = Damage * 0.75f;
            ghostHealth.CmdAdd(ghostHealValue);

            Debug.Log($"GhostAura {ghost.name}: Healed for {ghostHealValue}, Player healed for {healValue}");
        }
    }

    private void ApplyDamageToTarget()
    {
        Damage damage = new Damage
        {
            Value = Damage,
            Type = DamageType,
        };

        if (_targetCharacter != null) CmdApplyDamage(damage, _targetCharacter.gameObject);
        else if (_targetObject != null) CmdApplyDamage(damage, _targetObject.gameObject);
    }

    private void HealPlayer()
    {
        if (health == null) return;

        Heal heal = new Heal
        {
            Value = Damage,
        };

        health.CmdAdd(heal.Value);
    }

    //private Transform GetTargetTransform(IDamageable target)
    //{
    //    return (target as Component)?.transform;
    //}

    private bool TryGetTarget(out IDamageable target)
    {
        target = null;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _targetsLayers))
        {
            if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
            {
                target = damageable;
                return true;
            }
        }
        return false;
    }

    #region Talents

    public void SetPullingHealthGhostTalentActive(bool value)
    {
        pullingHealthGhostTalent = value;
    }

    public void PullingHealthSpeedWithSilenceTalentActive(bool value)
    {
        _pullingHealthSpeedWithSilenceTalent = value;
    }

    public void PullingHealthThroughGhosts(bool value)
    {
        _pullingHealthThroughGhosts = value;
    }

    #endregion

    private void HandleSkillCanceled()
    {
        if (_hero != null && _hero.Move != null)
        {
            Hero.Move.CanMove = true;
            Hero.Animator.speed = 1;
        }

        _targetCharacter = null;
        _targetObject = null;
        _targetPoint = Vector2.positiveInfinity;
        CmdStopShotSound();
    }

    [Command]
    private void CmdSyncGhosts(GameObject ghostObj)
    {
        ghost.Add(ghostObj);
    }

    //[Command]
    //private void CmdApplyDamage(GameObject targetObject, Damage damage, Skill skill)
    //{
    //    if (targetObject != null && targetObject.TryGetComponent<IDamageable>(out IDamageable target))
    //    {
    //        target.TryTakeDamage(ref damage, skill);
    //    }
    //}

    [Command]
    private void CmdSpawnPullingHealthEffect(GameObject startPoint, GameObject targetPoint)
    {
        if (_pullingHealthPrefab == null || startPoint == null || targetPoint == null) return;

        GameObject effectInstance = Instantiate(_pullingHealthPrefab, startPoint.transform.position, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(effectInstance, _hero.NetworkSettings.MyRoom);
        NetworkServer.Spawn(effectInstance);
        RpcInitEffects(effectInstance, startPoint, targetPoint);

        _allActiveEffects.Add(effectInstance);
        _activeEffect = effectInstance;
    }

    [Command]
    private void CmdSpawnPullingHealthEffectGhost(GameObject targetPoint)
    {
        if (_pullingHealthPrefab == null || targetPoint == null) return;

        for (int i = 0; i < ghost.Count; i++)
        {
            GameObject ghostEffectInstance = Instantiate(_pullingHealthPrefab, ghost[i].transform.position, Quaternion.identity);
            _activeGhostEffects.Add(ghostEffectInstance);
            SceneManager.MoveGameObjectToScene(ghostEffectInstance, _hero.NetworkSettings.MyRoom);
            NetworkServer.Spawn(ghostEffectInstance);
            RpcInitEffects(ghostEffectInstance, ghost[i], targetPoint);
        }
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

        _activeGhostEffects.Clear();

        ghost.Clear();

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

    [Command] 
    private void CmdCrossFade()
    {
        _hero.Animator.CrossFade("PullingHealthCastDelayExit", 0.1f);
    }

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
        ghost.Clear();
    }

    [Command]
    private void CmdPlayShotSound()
    {
        RpcPlayShotSound();
    }

    [Command]
    private void CmdStopShotSound()
    {
        RpcStopShotSound();
    }


    [ClientRpc]
    private void RpcPlayShotSound()
    {
        if (_audioSource != null && audioClip != null) _audioSource.PlayOneShot(audioClip);
    }

    [ClientRpc]
    private void RpcStopShotSound()
    {
        if (_audioSource != null) _audioSource.Stop();
    }

    protected override void ClearData()
    {
        Radius = _baseRadius;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _targetPoint = targetInfo.Points[0];
    }
}