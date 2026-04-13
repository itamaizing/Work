using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ShadowSkill : Skill
{
    [SerializeField] private ShadowMinion _shadowPrefab;
    [SerializeField] private float _shadowSpeedMultiplier = 0.5f;
    [SerializeField] private float _darkDamageThreshold   = 50f;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("Shadow");
    protected override bool IsCanCast => CheckCanCast();

    private float _accumulatedDarkDamage = 0f;
    private float _clickRadius = 0.5f;
    private Vector3 _clickPoint;

    #region SpiritHealthOnShadow
    private bool _spiritHealthIsEnabled;
    public bool EnableSpiritHealth(bool val) => _spiritHealthIsEnabled = val;
    #endregion

    private bool CheckCanCast() =>
        true;

    private bool IsEnemyTarget(Character target) =>
        target.gameObject.layer == LayerMask.NameToLayer("Enemy");
    
    protected override void Awake()
    {
        base.Awake();
        _currentChargers = 0;
        
        CheckChargers();
    }

    public void AnimCastShadow() => AnimStartCastCoroutine();
    public void AnimShadowEnd()  => AnimCastEnded();

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0)
            Targeting.SetTarget(targetInfo.GetTargets()[0] as Character);
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
        _clickPoint = Vector3.zero;
    }

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);
        Hero.DamageTracker.OnDamageTracked += TrackDarkDamage;
    }

    private void OnEnable()
    {
    }

    private void OnDisable()
    {
        Hero.DamageTracker.OnDamageTracked -= TrackDarkDamage;
    }

    [ClientRpc]
    private void TrackDarkDamage(Damage damage, GameObject target)
    {
        if(!isOwned) return;
        if (damage.School != Schools.Dark) return;

        _accumulatedDarkDamage += damage.Value;

        while (_accumulatedDarkDamage >= _darkDamageThreshold)
        {
            _accumulatedDarkDamage -= _darkDamageThreshold;
            AddCharge();

            if (Chargers > 0)
            {
                Disactive = false;
            }
        }
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        TargetInfo targetInfo = new TargetInfo();

        while (Targeting.GetTempTarget()?.Character == null)
        {
            if (GetMouseButton)
            {
                _clickPoint = Targeting.GetMousePoint();
                Targeting.FindTempTarget(_clickPoint, _clickRadius, canTargetSelf: false);

                if (Targeting.GetTempTarget()?.Character is Character character)
                {
                    if (!IsEnemyTarget(character))
                        Targeting.ClearTempTarget();
                }
            }
            yield return null;
        }

        Targeting.SetTarget(Targeting.GetTempTarget()?.Character);
        targetInfo.AddTarget(Targeting.GetTarget()?.Character);
        targetDataSavedCallback(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        var target = Targeting.GetTarget()?.Character;
        if (target == null) yield break;

        CmdSpawnShadow(target.gameObject);
        yield return null;
    }

    private void AddCharge()
    {
        if (_currentChargers < _maxCharges)
            Chargers = _currentChargers + 1;
        
        CheckChargers();
    }

    public void AddChargers(int num)
    {
        if (_currentChargers < _maxCharges)
        {
            Chargers = _currentChargers + num;
            if (Chargers > _maxCharges)
            {
                Chargers = _maxCharges;
            }
        }
        CheckChargers();
    }

    private void CheckChargers()
    {
        if (_currentChargers > 0)
        {
            Disactive = false;
        }
        else
        {
            Disactive = true;
        }
    }

    [Command]
    private void CmdSpawnShadow(GameObject targetGO)
    {
        if (!targetGO.TryGetComponent<Character>(out var target)) return;
        if (target.IsDead) return;

        ShadowMinion shadow = Instantiate(_shadowPrefab, transform.position, transform.rotation);

        NetworkServer.Spawn(shadow.gameObject, connectionToClient);

        TargetRpcInitShadow(connectionToClient, shadow.gameObject, targetGO, _shadowSpeedMultiplier);
    }
    
    public override bool TryUseCharge()
    {
        if (_currentChargers <= 0) return false;

        Chargers = _currentChargers - 1;

        CheckChargers();
        
        return true;
    }

    [TargetRpc]
    private void TargetRpcInitShadow(NetworkConnectionToClient conn, GameObject shadowGO, GameObject targetGO, float speedMultiplier)
    {
        if (shadowGO == null || targetGO == null) return;

        var shadow = shadowGO.GetComponent<ShadowMinion>();
        var target = targetGO.GetComponent<Character>();

        if (shadow == null || target == null) return;

        shadow.InitOnClient(target, this, speedMultiplier, applyShackleOnExpire: true);
        shadow.IsApplySpiritHealth(_spiritHealthIsEnabled);
    }
}
