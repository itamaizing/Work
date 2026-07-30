using System.Collections;
using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System;
using System.Linq;

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
    
    #region TentacleLifeTimeTalent
    private bool _isExtendDurationOnDamageTalent = false;
    
    public void ExtendDurationOnDamageTalent(bool value)
    {
        if(_isExtendDurationOnDamageTalent == value) return;
        
        _isExtendDurationOnDamageTalent = value;
        CmdEnableExtendDurationOnDamage(_isExtendDurationOnDamageTalent);
    }

    [Command]
    private void CmdEnableExtendDurationOnDamage(bool value)
    {
        _isExtendDurationOnDamageTalent = value;
    }
    
    private readonly List<TentacleProjectile> _activeTentacles = new List<TentacleProjectile>();
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

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);
        
        SubscribeCharacterSkills(_hero.gameObject);

        if (_spawnComponent != null)
        {
            _spawnComponent.UnitAdded += OnUnitAdded;
            _spawnComponent.UnitRemoved += OnUnitRemoved;
        }
    }

    private void OnDisable()
    {
        OnSkillCanceled -= HandleSkillCanceled;

        UnsubscribeCharacterSkills(_hero.gameObject);

        if (_spawnComponent != null)
        {
            _spawnComponent.UnitAdded -= OnUnitAdded;
            _spawnComponent.UnitRemoved -= OnUnitRemoved;
        }
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
        if(isClient)
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
        Character lockedTarget = null;
        Vector3 targetPoint = Vector3.positiveInfinity;

        if (!_isPlacingTentacles)
        {
            _previewInstance = Instantiate(_tentaclesPreview, Targeting.GetMousePoint(), Quaternion.identity);
            _previewInstance.IsAttractionTentacle = _isAttractionTentacleTalent;
        }

        while (float.IsPositiveInfinity(targetPoint.x))
        {
            Vector3 mousePoint = Targeting.GetMousePoint();

            if (_previewInstance != null)
                _previewInstance.transform.position = mousePoint;

            if (GetMouseButton)
            {
                if (_isProtectiveCooconSpawn)
                {
                    TargetData allyTarget = Targeting.GetTargetOrPoint();
                    if (allyTarget != null && allyTarget.Type == TargetType.Object && allyTarget.Character != null)
                    {
                        if (IsAlly(allyTarget.Character))
                        {
                            _spawnPoint = allyTarget.Character.transform.position;
                            SetQueueTarget(allyTarget, callbackDataSaved);
                            CleanupPreviewInstances();
                            yield break;
                        }
                    }
                }

                if (_isAttractionTentacleTalent)
                {
                    TargetData enemyTarget = Targeting.GetTargetOrPoint();

                    if (enemyTarget != null && enemyTarget.Type == TargetType.Object &&
                        IsValidEnemy(enemyTarget.Character))
                    {
                        _isPlacingTentacles = true;
                        lockedTarget = enemyTarget.Character;
                        targetPoint = lockedTarget.transform.position;

                        SetupAttractionPreview(lockedTarget);
                        yield return _waitForSeconds;
                        break;
                    }
                    else if (_isWombSpawning)
                    {
                        targetPoint = mousePoint;
                        yield return _waitForSeconds;
                        break;
                    }
                }
                else
                {
                    List<TargetData> targets = Targeting.FindTargets(mousePoint, _radiusTarget, canTargetSelf: false);
                    TargetData validEnemy = targets?.FirstOrDefault(t => IsValidEnemy(t.Character));

                    if (validEnemy != null)
                    {
                        _isPlacingTentacles = true;
                        lockedTarget = validEnemy.Character;
                        targetPoint = lockedTarget.transform.position;

                        if (_previewInstance != null)
                            _previewInstance.transform.SetParent(lockedTarget.transform);

                        yield return _waitForSeconds;
                        break;
                    }
                    else if (_isWombSpawning)
                    {
                        targetPoint = mousePoint;
                        yield return _waitForSeconds;
                        break;
                    }
                }
            }

            yield return null;
        }

        if (!_isAttractionTentacleTalent && lockedTarget != null)
            targetPoint = lockedTarget.transform.position;

        if (_isAttractionTentacleTalent && lockedTarget != null)
        {
            while (true)
            {
                Vector3 hoverPoint = Targeting.GetMousePoint();
                Vector3 targetCenter = lockedTarget.transform.position;

                Vector3 offset = hoverPoint - targetCenter;

                float attractionRadius = _previewInstance != null ? _previewInstance.Radius : AreaInfo.Radius;
                if (offset.magnitude > attractionRadius)
                {
                    offset = offset.normalized * attractionRadius;
                }

                Vector3 potentialSpawnPoint = targetCenter + offset;

                if (_previewInstancePrefab != null)
                {
                    _previewInstancePrefab.transform.position = potentialSpawnPoint;
                }

                if (GetMouseButton)
                {
                    if (Targeting.NoObstacles(potentialSpawnPoint, targetCenter, _obstacle) &&
                        IsValidVector(potentialSpawnPoint))
                    {
                        targetPoint = potentialSpawnPoint;
                        yield return _waitForSeconds;
                        break;
                    }
                }

                yield return null;
            }
        }

        TrySpendAttackingPsi();
        CleanupPreviewInstances();

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Points.Add(targetPoint);
        if (lockedTarget != null)
            targetInfo.AddTarget(lockedTarget);

        callbackDataSaved(targetInfo);
    }

    private bool IsAlly(Character character)
    {
        if (character == null) return false;
        return ((1 << character.gameObject.layer) & _alliesMask) != 0;
    }

    private void SetupAttractionPreview(Character target)
    {
        if (_previewInstance != null) 
            _previewInstance.transform.SetParent(target.transform);

        _previewInstancePrefab = Instantiate(_tentaclesPreview, _previewInstance.transform.position, Quaternion.identity);
        _previewInstancePrefab.Tentacle.SetActive(true);
        _previewInstancePrefab.IsPreview = false;
    }

    private void CleanupPreviewInstances()
    {
        if (_previewInstance != null) Destroy(_previewInstance.gameObject);
        if (_previewInstancePrefab != null) Destroy(_previewInstancePrefab.gameObject);
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
        
        if (_isExtendDurationOnDamageTalent)
        {
            _activeTentacles.Add(_currentTentacle);
        }
        
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
    
    #region DamageTrackingTentacles
    
    private void OnUnitAdded(Character unit)
    {
        SubscribeCharacterSkills(unit.gameObject);
    }

    private void OnUnitRemoved(Character unit)
    {
        UnsubscribeCharacterSkills(unit.gameObject);
    }
    
    private void SubscribeCharacterSkills(GameObject target)
    {
        if (target.TryGetComponent(out Character character))
        {
            foreach (var skill in character.Abilities.Abilities)
            {
                if (skill != null)
                {
                    skill.OnBeforeApplyDamage += HandleSkillDamage; 
                }
            }
        }
    }

    private void UnsubscribeCharacterSkills(GameObject target)
    {
        if (target.TryGetComponent(out Character character))
        {
            foreach (var skill in character.Abilities.Abilities)
            {
                if (skill != null)
                {
                    skill.OnBeforeApplyDamage -= HandleSkillDamage; 
                }
            }
        }
    }


    private void HandleSkillDamage(ref Damage damage,Skill skill, GameObject target)
    {
        if (!_isExtendDurationOnDamageTalent || damage.Value <= 0) return;

        _activeTentacles.RemoveAll(t => t == null);

        foreach (var tentacle in _activeTentacles)
        {
            tentacle.ExtendLifeTime(damage.Value);
        }
    }
    
    #endregion
}