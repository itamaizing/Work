using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class Ghost : Skill
{
    [Header("Ghost Settings")]
    [SerializeField] private float defaultRadius = 3f;
    [SerializeField] private float extendedRadius = 5f;
    [SerializeField] private float teleportManaUse = 6f;
    [SerializeField] private int maxGhosts = 2;
    [SerializeField] private MinionComponent ghostPrefab;
    [SerializeField] private GameObject ghostPrefabPreview;
    [SerializeField] private GameObject way;
    [SerializeField] private bool cooldownGhostShotActive;
    [SerializeField] private AudioClip aCTeleportToGhost;
    [SerializeField] private AudioClip aCÑontrolGhostToTarget;
    [SerializeField] private AudioClip aCSummoningGhost;

    private GameObject _ghostPrefabPreview;
    private AudioSource _audioSource;
    private List<Character> _ghosts;
    private SpawnComponent _spawnComponent;
    private float _baseCastDelay;
    private bool _ghostMoveToTarget;
    private bool _shouldSpawnGhost;
    private bool _teleportGhost;
    private bool _sendingGhostTargetTalentActive;
    private Vector3 _spawnPosition;
    private Character _ghostToMove;
    private Character _targetCharacter;
    private Character _ghostToTeleport;

    protected override int AnimTriggerCastDelay => Animator.StringToHash("GhostCastDelay");
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => IsCooldowned && IsHaveCharge;

    public bool CooldownGhostShotActive => cooldownGhostShotActive;

    protected override void Awake()
    {
        _audioSource = GetComponent<AudioSource>();

        base.Awake();
        InitializeFields();
        RegisterSpawnEvents();
    }

    private void OnDestroy()
    {
        UnregisterSpawnEvents();
    }

    private void InitializeFields()
    {
        Radius = defaultRadius;
        _baseCastDelay = CastDeley;
        _ghosts = new List<Character>();
        _spawnComponent = GetComponent<SpawnComponent>();
    }

    private void SetRadius(float radius)
    {
        Radius = radius;
        _skillRender.DrawRadius(Radius);
    }

    private void RegisterSpawnEvents()
    {
        if (_spawnComponent != null)
        {
            _spawnComponent.UnitAdded += OnGhostSpawned;
        }
    }

    private void UnregisterSpawnEvents()
    {
        if (_spawnComponent != null)
        {
            _spawnComponent.UnitAdded -= OnGhostSpawned;
        }
    }

    protected override IEnumerator PrepareJob()
    {
        Vector3 mousePositionStart = GetMousePoint();

        _ghostPrefabPreview = Instantiate(ghostPrefabPreview, mousePositionStart, Quaternion.identity);

        while (!_disactive)
        {
            Vector3 mousePosition = GetMousePoint();
            _ghostPrefabPreview.transform.position = mousePosition;

            if (_sendingGhostTargetTalentActive && IsMouseOverTarget(out Character character) && character.CharacterState.CheckForState(States.InnerDarkness))
            {
                if (Input.GetMouseButtonDown(0) && IsWithinRadius(character.transform.position, defaultRadius) && !GetComponent<GhostAura>())
                {
                    if (_ghosts.Count > 0)
                    {
                        _ghostToMove = _ghosts.Count > 1 ? _ghosts[_ghosts.Count - 2] : _ghosts[0];
                        _targetCharacter = character;
                        _ghostMoveToTarget = true;
                    }

                    yield break;
                }
            }

           else if (IsMouseOverGhost(out Character ghost) && ghost.GetComponent<GhostAura>())
            {
                SetRadius(extendedRadius);

                if (Input.GetMouseButtonDown(0) && IsWithinRadius(ghost.transform.position, extendedRadius))
                {
                    _ghostToTeleport = ghost;
                    _teleportGhost = true;
                    TeleportToGhost(_ghostToTeleport);
                    continue;
                }
            }

            else
            {
                SetRadius(defaultRadius);

                if (Input.GetMouseButtonDown(0) && IsMouseInRadius(Radius))
                {
                    _spawnPosition = GetMousePoint();
                    _shouldSpawnGhost = true;

                    AdjustCastDelay();
                    yield break;
                }
            }

            yield return null;
        }

        if (_ghostPrefabPreview != null) Destroy(_ghostPrefabPreview);
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
                float randomFearDuration = Random.Range(0.4f, 0.6f) * innerDarknessStacks;
                characterState.AddState(States.Fear, randomFearDuration, 0, gameObject, "Ghost");
                //CmdAddFear(characterState, randomFearDuration);
            }

            else if (innerDarknessStacks == 0) characterState.AddState(States.Fear, Random.Range(0.4f, 0.6f), 0, gameObject, "Ghost");
        }

        CmdAcÑontrolGhostToTarget();
        _ghosts.Remove(ghost);
        Destroy(ghost.gameObject);
    }


    //[Command]
    //private void CmdAddFear(CharacterState characterState, float randomFearDuration)
    //{
    //    characterState.AddState(States.Fear, randomFearDuration, 0, gameObject, "Ghost");
    //}


    #endregion

    private void AdjustCastDelay()
    {
        if (_ghosts.Count == 0) _castDeley = _baseCastDelay * 0.5f;

        else if (_ghosts.Count >= maxGhosts) _castDeley = _baseCastDelay * 2f;
    }

    private void TeleportToGhost(Character ghost)
    {
        if (ghost == null || !(ghost is MinionComponent)) return;

        CmdAcTeleportToGhost();
        ReduceSkillCosts();
        ActivateWayIndicator();
        PerformTeleport(ghost.transform.position);
        RemoveGhost(ghost);
        RestoreSkillCosts();

        _teleportGhost = false;
    }

    private void ReduceSkillCosts()
    {
        foreach (var skillCost in _skillEnergyCosts)
        {
            skillCost.resourceCost *= 0.5f;
        }
    }

    private void RestoreSkillCosts()
    {
        foreach (var skillCost in _skillEnergyCosts)
        {
            skillCost.resourceCost *= 2f;
        }
    }

    private void ActivateWayIndicator()
    {
        way.SetActive(true);
    }

    private void PerformTeleport(Vector3 targetPosition)
    {
        var moveComponent = GetComponent<MoveComponent>();
        moveComponent?.TeleportToPositionSmooth(targetPosition, 0.5f);
        StartCoroutine(DisableWayAfterTeleport(moveComponent, targetPosition));
    }

    private void RemoveGhost(Character ghost)
    {
        if (!(ghost is MinionComponent minion)) return;

        _spawnComponent.CmdRemoveUnit(minion);
        _ghosts.Remove(ghost);
    }

    private IEnumerator DisableWayAfterTeleport(MoveComponent moveComponent, Vector3 targetPosition)
    {
        while (Vector3.Distance(moveComponent.transform.position, targetPosition) > 0.1f)
        {
            yield return null;
        }
        way.SetActive(false);
    }

    private void SpawnGhost(Vector3 position, Quaternion LookRotation)
    {
        if (_spawnComponent == null) return;

        _spawnComponent.CmdSpawnUnitPoint(position, LookRotation);
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
            else
            {
                Debug.LogWarning("SpawnGhost: No valid ghost found to remove.");
            }
        }
    }

    private void OnGhostSpawned(Character ghost)
    {
        if (ghost == null || _ghosts.Contains(ghost) || !(ghost is MinionComponent)) return;
        _ghosts.Add(ghost);
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

    private bool IsWithinRadius(Vector3 targetPosition, float radius)
    {
        return Vector3.Distance(transform.position, targetPosition) <= radius;
    }

    private bool TryConsumeMana(float amount)
    {
        var manaResource = _hero.Resources.FirstOrDefault(r => r.Type == ResourceType.Mana);
        if (manaResource != null && manaResource.CurrentValue >= amount)
        {
            manaResource.CmdUse(amount);
            return true;
        }

        return false;
    }

    private IEnumerator SpawnGhostVisualEffect(Vector3 targetPosition)
    {
        RemoveOldestGhostIfNeeded();

        CmdAcSummoningGhost();

        Vector3 spawnDirection = (targetPosition - transform.position).normalized;
        float offsetDistance = Radius - 1;
        Vector3 spawnStartPosition = targetPosition - spawnDirection * offsetDistance;

        var ghostVisual = Instantiate(ghostPrefab, spawnStartPosition, Quaternion.identity);

        if (ghostVisual.TryGetComponent<Collider>(out var collider))
        {
            collider.enabled = false;
        }

        yield return MoveGhostToTarget(ghostVisual, targetPosition);
    }

    private IEnumerator MoveGhostToTarget(MinionComponent ghostVisual, Vector3 targetPosition)
    {
        float moveDuration = 1.0f;
        float elapsedTime = 0f;
        Vector3 startPosition = ghostVisual.transform.position;

        Vector3 direction = (targetPosition - startPosition).normalized;
        if (direction != Vector3.zero)
        {
            ghostVisual.transform.rotation = Quaternion.LookRotation(direction);
        }

        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveDuration;
            ghostVisual.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        _shouldSpawnGhost = false;
        Destroy(ghostVisual.gameObject);
        SpawnGhost(_spawnPosition, ghostVisual.transform.rotation);
    }


    protected override IEnumerator CastJob()
    {
        if (_shouldSpawnGhost && _spawnPosition != Vector3.zero) StartCoroutine(SpawnGhostVisualEffect(_spawnPosition));
        else if (_ghostMoveToTarget && _ghostToMove != null && _targetCharacter != null) StartCoroutine(MoveGhostToCharacter(_ghostToMove, _targetCharacter));

        yield break;
    }

    protected override void ClearData()
    {
        Radius = defaultRadius;
        if (_ghostPrefabPreview != null) Destroy(_ghostPrefabPreview);
    }

    [Command]
    private void CmdAcSummoningGhost()
    {
        RpcAcSummoningGhost();
    }

    [Command]
    private void CmdAcTeleportToGhost()
    {
        RpcAcTeleportToGhost();
    }

    [Command]
    private void CmdAcÑontrolGhostToTarget()
    {
        RpcAcÑontrolGhostToTarget();
    }


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
    private void RpcAcÑontrolGhostToTarget()
    {
        if (_audioSource != null && aCÑontrolGhostToTarget != null) _audioSource.PlayOneShot(aCÑontrolGhostToTarget);
    }

    #region SendingGhostTargetTalentActive
    public void SendingGhostTargetTalentActive(bool value)
    {
        _sendingGhostTargetTalentActive = value;
    }
    #endregion

    #region CooldownGhostShotActiveTalent
    public void CooldownGhostShotActiveTalent(bool value)
    {
        cooldownGhostShotActive = value;
    }
    #endregion
}