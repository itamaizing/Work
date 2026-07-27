using System.Collections;
using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System;

public class Tentacles : Skill
{
    [SerializeField] private Character _player;
    [SerializeField] private TentacleProjectile _tentaclesPrefab;
    [SerializeField] private TentacleProjectile _tentaclesPreview;
    [SerializeField] private ProtectiveCocoon _protectiveCocoonPrefab;
    [SerializeField] private AttackingPsionicEnergy _attackingPsionicEnergy;
    [SerializeField] private SpawnComponent _spawnComponent;
    [SerializeField] private SummoningSwarm _summoningSwarm;
    [SerializeField] private float _radiusTarget = 0.5f;

    private bool _isPlacingTentacles = false;
    private bool _isClickedOnGround = false;
    private bool _usedSwarmCharge;

    private Vector3 _spawnPoint = Vector3.positiveInfinity;
    private HashSet<Character> _charactersInPreview = new HashSet<Character>();

    private Character _lockedTarget;
    private TentacleProjectile _previewInstance;
    private TentacleProjectile _previewInstancePrefab;
    private TentacleProjectile _currentTentacle;
    private Coroutine _radiusUpdateCoroutine;
    private float _spentAttackingPsiEnergy;
    private WaitForSeconds _waitForSeconds;

    #region Const
    private const float AttractionSphereCastRadius = 0.1f;
    private const float UpdateRadiusColorSphereCastRadius = 500f;
    private const float AttractionMaxCastDistance = 100f;
    private const float WaitForSecondsTick = 0.1f;
    #endregion

    #region Talent
    private bool _isPsionicsTalentThree = false;
    private bool _isAttractionTentacleTalent = false;
    private bool _isProtectiveCooconSpawn = false;
    private bool _isProtectiveCooconSpawnAttack = false;
    private bool _isSpawnSpike = false;

    public event Action<bool> OnSpawnGetomirChanged;
    public event Action<bool> OnWombSpreadsMucusChanged;
    public event Action<bool> OnWombSpreadsParasitesChanged;

    public void ProtectiveCooconSpawn(bool value) => _isProtectiveCooconSpawn = value;
    public void PsionicsTalentThree(bool value) => _isPsionicsTalentThree = value;
    public void AttractionTentacleTalent(bool value) => _isAttractionTentacleTalent = value;
    public void ProtectiveCooconSpawnAttack(bool value) => _isProtectiveCooconSpawnAttack = value;
    public void SpawnSpike(bool value) => _isSpawnSpike = value;

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

    #region WombSpawning

    private bool _isWombSpawning;
    
    public void ActivateWombSpawning(bool value)
    {
        if (value == _isWombSpawning) return;
        _isWombSpawning = value;
    }

    #endregion
    
    #endregion

    public TentacleProjectile CurrentTentacle { get => _currentTentacle; set => _currentTentacle = value; }

    private LayerMask _alliesMask;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("Spell");

    protected override bool IsCanCast =>
        _summoningSwarm != null &&
        _spawnPoint != Vector3.positiveInfinity &&
        IsCanRadius() &&
        (Targeting.GetTarget()?.Character != null || _isWombSpawning);

    protected override void UseCooldownOrCharges()
    {
        bool hasSwarmCharges = _summoningSwarm != null && _summoningSwarm.ChargesSwarm > 0;

        if (hasSwarmCharges)
        {
            _summoningSwarm.UseSwarmCharges(1);
            return;
        }

        base.UseCooldownOrCharges();
    }

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
        _waitForSeconds = new WaitForSeconds(WaitForSecondsTick);
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

    private bool IsValidEnemy(Character character)
    {
        if (character == null) return false;
        if (((1 << character.gameObject.layer) & _alliesMask) != 0) return false;
        if (((1 << character.gameObject.layer) & Targeting.Layer) == 0) return false;
        return true;
    }

    protected override void ClearData()
    {
        _skillRender.IsOverrideClosestTarget = false;
        _isClickedOnGround = false;
        _skillRender.StopDrawRadius();

        _isPlacingTentacles = false;
        _spawnPoint = Vector3.positiveInfinity;
        Targeting.ClearTarget();
        _spentAttackingPsiEnergy = 0f;
        Hero.Move.SetCanMove(true);
        _player.Move.StopLookAt();

        if (_previewInstance != null) Destroy(_previewInstance.gameObject);
        if (_previewInstancePrefab != null) Destroy(_previewInstancePrefab.gameObject);

        if (_radiusUpdateCoroutine != null)
        {
            StopCoroutine(_radiusUpdateCoroutine);
            _radiusUpdateCoroutine = null;
        }
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        _skillRender.IsOverrideClosestTarget = true;
        _lockedTarget = null;

        Vector3 mousePositionStart = Targeting.GetMousePoint();
        Vector3 targetPoint = Vector3.positiveInfinity;

        if (!_isPlacingTentacles)
        {
            _previewInstance = Instantiate(_tentaclesPreview, mousePositionStart, Quaternion.identity);
            _previewInstance.IsAttractionTentacle = _isAttractionTentacleTalent;
            _skillRender.DrawRadius(AreaInfo.Radius);
            _radiusUpdateCoroutine = StartCoroutine(UpdateRadiusColor());
        }

        while (float.IsPositiveInfinity(targetPoint.x))
        {
            if (GetMouseButton && Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit clickHit))
            {
                Character clickedCharacter = clickHit.collider.GetComponentInParent<Character>();

                if (_isProtectiveCooconSpawn && clickedCharacter != null && ((1 << clickedCharacter.gameObject.layer) & _alliesMask) != 0)
                {
                    _spawnPoint = clickedCharacter.transform.position;

                    TargetInfo allyInfo = new TargetInfo();
                    allyInfo.Points.Add(_spawnPoint);
                    allyInfo.AddTarget(clickedCharacter);

                    Targeting.SetTarget(clickedCharacter);

                    _lockedTarget = null;
                    _isPlacingTentacles = false;

                    if (_previewInstance != null)
                    {
                        Destroy(_previewInstance.gameObject);
                        _previewInstance = null;
                    }

                    if (_previewInstancePrefab != null)
                    {
                        Destroy(_previewInstancePrefab.gameObject);
                        _previewInstancePrefab = null;
                    }

                    callbackDataSaved(allyInfo);
                    yield break;
                }
            }

            Vector3 mousePoint = Targeting.GetMousePoint();

            if (_previewInstance != null) _previewInstance.transform.position = mousePoint;

            if (GetMouseButton && !_isPlacingTentacles)
            {
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hitTarget))
                {
                    if (_isAttractionTentacleTalent && hitTarget.collider.TryGetComponent<Character>(out Character character) && IsValidEnemy(character))
                    {
                        float distToHero = Vector3.Distance(Hero.transform.position, character.transform.position);

                        if (distToHero > AreaInfo.Radius)
                        {
                            yield return null;
                            continue;
                        }

                        _isPlacingTentacles = true;
                        _lockedTarget = character;

                        if (_previewInstance != null) _previewInstance.transform.SetParent(_lockedTarget.transform);

                        _previewInstancePrefab = Instantiate(_tentaclesPreview, _previewInstance.transform.position, Quaternion.identity);
                        _previewInstancePrefab.Tentacle.SetActive(true);
                        _previewInstancePrefab.IsPreview = false;
                        targetPoint = character.transform.position;

                        yield return _waitForSeconds;
                        break;
                    }
                    else
                    {
                        bool foundEnemy = false;

                        Collider[] colliders = Physics.OverlapSphere(mousePoint, _radiusTarget);
                        foreach (var collider in colliders)
                        {
                            if (!collider.TryGetComponent<Character>(out Character targetHit)) continue;

                            if (IsValidEnemy(targetHit))
                            {
                                float distToHero = Vector3.Distance(Hero.transform.position, targetHit.transform.position);

                                if (distToHero > AreaInfo.Radius)
                                {
                                    yield return null;
                                    continue;
                                }

                                _isPlacingTentacles = true;
                                _lockedTarget = targetHit;
                                _previewInstance.transform.SetParent(_lockedTarget.transform);

                                if (_isAttractionTentacleTalent)
                                {
                                    _previewInstancePrefab = Instantiate(_tentaclesPreview, _previewInstance.transform.position, Quaternion.identity);
                                    _previewInstancePrefab.Tentacle.SetActive(true);
                                    _previewInstancePrefab.IsPreview = false;
                                }

                                targetPoint = targetHit.transform.position;
                                yield return _waitForSeconds;
                                foundEnemy = true;
                                break;
                            }
                        }

                        if (!foundEnemy)
                        {
                            if (_isWombSpawning)
                            {
                                float distToGround = Vector3.Distance(Hero.transform.position, mousePoint);
                                if (distToGround <= AreaInfo.Radius)
                                {
                                    targetPoint = mousePoint;
                                    yield return _waitForSeconds;
                                    break;
                                }
                            }

                            yield return null;
                            continue;
                        }
                    }
                }
            }

            yield return null;
        }

        if (!_isAttractionTentacleTalent && _lockedTarget != null) targetPoint = _lockedTarget.transform.position;

        if (_isAttractionTentacleTalent && _lockedTarget != null)
        {
            while (true)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (GetMouseButton)
                {
                    if (Cooldown.IsActive)
                    {
                        yield return null;
                        continue;
                    }

                    if (Physics.SphereCast(ray, AttractionSphereCastRadius, out hit, AttractionMaxCastDistance, _obstacle))
                    {
                        yield return null;
                        continue;
                    }

                    if (Physics.Raycast(ray, out hit, Mathf.Infinity))
                    {
                        Vector3 groundPoint = hit.point;

                        Vector3 direction = groundPoint - _previewInstance.transform.position;
                        float distanceToCaster = direction.magnitude;

                        if (distanceToCaster > _previewInstance.Radius)
                            direction = direction.normalized * _previewInstance.Radius;

                        if (_previewInstancePrefab != null)
                            _previewInstancePrefab.transform.position = _previewInstance.transform.position + direction;

                        float distanceToTarget = Vector3.Distance(_previewInstancePrefab.transform.position, transform.position);

                        if (distanceToTarget <= AreaInfo.Radius)
                        {
                            Vector3 potentialSpawnPoint = _previewInstancePrefab.transform.position;

                            if (!IsValidVector(potentialSpawnPoint))
                            {
                                yield return null;
                                continue;
                            }

                            targetPoint = potentialSpawnPoint;
                            break;
                        }
                    }
                }

                if (_previewInstancePrefab != null)
                {
                    if (Physics.Raycast(ray, out hit, Mathf.Infinity))
                    {
                        Vector3 hoverPoint = hit.point;

                        Vector3 dir = hoverPoint - _previewInstance.transform.position;
                        float distance = dir.magnitude;

                        if (distance > _previewInstance.Radius) dir = dir.normalized * _previewInstance.Radius;

                        _previewInstancePrefab.transform.position = _previewInstance.transform.position + dir;
                    }
                }

                yield return null;
            }
        }

        if (_lockedTarget != null)
            Targeting.SetTarget(_lockedTarget);

        TrySpendAttackingPsi();
        if (_previewInstance != null) Destroy(_previewInstance.gameObject);

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Points.Add(targetPoint);
        if (Targeting.GetTarget()?.Character != null)
            targetInfo.AddTarget(Targeting.GetTarget()?.Character);

        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (!IsValidVector(_spawnPoint)) yield break;

        Character targetCharacter = Targeting.GetTarget()?.Character;

        bool isAlly = targetCharacter != null && ((1 << targetCharacter.gameObject.layer) & _alliesMask) != 0;

        if (_isProtectiveCooconSpawn && isAlly)
        {
            CmdSpawnProtectiveCocoon(targetCharacter);
            ClearData();
            yield break;
        }

        if (targetCharacter != null)
        {
            float distance = Vector3.Distance(_spawnPoint, targetCharacter.transform.position);

            float tentacleRange = _previewInstancePrefab != null ? _previewInstancePrefab.Radius : AreaInfo.Radius;

            if (distance > tentacleRange)
            {
                TryCancel(true);
                Hero.Move.SetCanMove(true);
                yield break;
            }

            CmdSpawnTentacles(_spawnPoint, targetCharacter, _spentAttackingPsiEnergy);
        }
        else if (_isWombSpawning)
        {
            _hero.Abilities.GetSkill<WombSpawn>().SpawnWombExternal(_spawnPoint);
        }

        ClearData();
        _skillRender.StopDrawRadius();
        yield return null;
    }
    
    private void SpawnWomb(Vector3 position)
    {
        if (!IsValidVector(position) || _spawnComponent == null) return;
        _spawnComponent.CmdSpawnEnemyPoint(position, Quaternion.identity, null, 0, false, Hero);
    }

    private IEnumerator UpdateRadiusColor()
    {
        while (true)
        {
            bool isPreviewInsideRadius = false;
            bool isCharacterInsidePreview = false;

            HashSet<Character> newCharactersInPreview = new HashSet<Character>();

            if (_previewInstance != null)
            {
                Collider[] hitColliders = Physics.OverlapSphere(_previewInstance.transform.position, AreaInfo.Area + UpdateRadiusColorSphereCastRadius);

                foreach (var hitCollider in hitColliders)
                {
                    if (hitCollider.TryGetComponent<Character>(out Character character) && character != _player)
                    {
                        float distanceToCharacter = Vector3.Distance(_previewInstance.transform.position, character.transform.position);

                        if (distanceToCharacter <= AreaInfo.Area)
                        {
                            isCharacterInsidePreview = true;
                            character.SelectedCircle.SwitchClostestTarget(true);
                        }
                        else
                        {
                            character.SelectedCircle.SwitchClostestTarget(false);
                        }

                        newCharactersInPreview.Add(character);
                    }
                }

                if (_lockedTarget == null)
                {
                    float distanceToPreview = Vector3.Distance(transform.position, _previewInstance.transform.position);
                    isPreviewInsideRadius = distanceToPreview <= (AreaInfo.Radius + _previewInstance.Radius);
                }
            }

            if (_previewInstancePrefab != null && _lockedTarget != null)
            {
                float distanceToPrefab = Vector3.Distance(transform.position, _previewInstancePrefab.transform.position);
                isPreviewInsideRadius = distanceToPrefab <= AreaInfo.Radius;
            }

            if (_previewInstance != null) _previewInstance.SetRadiusColor(isCharacterInsidePreview ? Color.green : Color.red);
            _skillRender.DrawRadiusColor(AreaInfo.Radius, isPreviewInsideRadius ? Color.green : Color.red);

            _charactersInPreview = newCharactersInPreview;

            yield return _waitForSeconds;
        }
    }

    private void TrySpendAttackingPsi()
    {
        if (_attackingPsionicEnergy.IsAttackingPsiEnergy && _attackingPsionicEnergy.CurrentValue > 0f)
        {
            _spentAttackingPsiEnergy = _attackingPsionicEnergy.CurrentValue;
            CmdUseAttackingEnergy(_attackingPsionicEnergy.CurrentValue);
        }
    }

    private bool TryClickHero(out Character hero)
    {
        hero = null;

        if (!GetMouseButton)
            return false;

        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
        {
            if (hit.collider.TryGetComponent<Character>(out Character character))
            {
                if (character == Hero)
                {
                    hero = character;
                    return true;
                }
            }
        }

        return false;
    }

    [Command]
    private void CmdSpawnProtectiveCocoon(Character target)
    {
        if (target == null) return;

        Vector3 spawnPos = target.transform.position;

        var cocoon = Instantiate(_protectiveCocoonPrefab, spawnPos, Quaternion.identity);
        NetworkServer.Spawn(cocoon.gameObject);

        int damage = 0;

        if (_attackingPsionicEnergy != null)
        {
            float availableEnergy = _attackingPsionicEnergy.CurrentValue;
            damage = Mathf.FloorToInt(availableEnergy / 2f);

            float energyToSpend = damage * 2f;
            _attackingPsionicEnergy.CurrentValue -= energyToSpend;
        }

        cocoon.Init(target, this, _isProtectiveCooconSpawnAttack, damage);

        RpcInitProtectiveCocoon(cocoon.gameObject, target, damage);
    }

    [Command]
    private void CmdSpawnTentacles(Vector3 position, Character target, float _spentAttackingPsiEnergy)
    {
        if (!IsValidVector(position)) return;
        if (target == null) return;

        _currentTentacle = Instantiate(_tentaclesPrefab, position, Quaternion.identity);

        _currentTentacle.Init(_player, target, position, target.transform.position, true, _isPsionicsTalentThree, _isAttractionTentacleTalent, _isSpawnSpike, _spentAttackingPsiEnergy, this);

        NetworkServer.Spawn(_currentTentacle.gameObject);
        RpcInitTentacles(_currentTentacle.gameObject, target, position, _spentAttackingPsiEnergy);

        if (_radiusUpdateCoroutine != null)
        {
            StopCoroutine(_radiusUpdateCoroutine);
            _radiusUpdateCoroutine = null;
        }
    }

    [Command]
    private void CmdUseAttackingEnergy(float value)
    {
        _attackingPsionicEnergy.CurrentValue -= value;
    }

    [ClientRpc]
    private void RpcInitTentacles(GameObject tentacleObject, Character target, Vector3 position, float _spentAttackingPsiEnergy)
    {
        if (!IsValidVector(position)) return;
        if (tentacleObject == null) return;

        tentacleObject.GetComponent<TentacleProjectile>().Init(_player, target, position, target.transform.position, true, _isPsionicsTalentThree, _isAttractionTentacleTalent, _isSpawnSpike, _spentAttackingPsiEnergy, this);
    }

    [ClientRpc]
    private void RpcInitProtectiveCocoon(GameObject cocoonObject, Character target, float damage)
    {
        if (cocoonObject == null || target == null) return;

        var cocoon = cocoonObject.GetComponent<ProtectiveCocoon>();
        cocoon.Init(target, this, _isProtectiveCooconSpawnAttack, damage);
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _spawnPoint = targetInfo.Points[0];
        if (targetInfo.GetTargets().Count > 0) Targeting.SetTarget((Character)targetInfo.GetTargets()[0]);
    }
}