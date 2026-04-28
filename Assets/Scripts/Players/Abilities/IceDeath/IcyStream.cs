using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class IcyStream : Skill
{
    public struct IcyStreamState
    {
        public Character Target;
        public int CurrentTick;
        public int MaxTicks;
    }

    [Header("Stream Settings")]
    [SerializeField] private float _tickInterval = 0.3f;
    [SerializeField] private Transform _streamStartPoint;

    [Header("Visual")]
    [SerializeField] private GameObject _icyStreamPrefab;

    [SerializeField] private float _runeCost = 1f;
    [SerializeField] private float _energyPerTick = 5f;

    private Character _cachedTarget;
    private Coroutine _streamCoroutine;
    private GameObject _activeEffect;

    private bool _isStreaming;
    private int _currentTick;
    private const int MaxTicks = 7;

    private const float FrostEnergyCoolingBonusPerStack = 1f;

    protected override bool IsCanCast => !_isStreaming && Targeting.GetTarget() != null && Vector3.Distance(Targeting.GetTarget().Transform.position, transform.position) <= AreaInfo.Radius && HasEnoughResourcesToStart();

    private bool HasEnoughResourcesToStart()
    {
        var energy = Hero.Resources[ResourceType.Energy];
        var rune = Hero.Resources[ResourceType.Rune];

        float minEnergy = _energyPerTick;

        return energy.CurrentValue >= minEnergy && rune.CurrentValue >= _runeCost;
    }

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    public IcyStreamState CurrentState { get; private set; }

    private void OnEnable()
    {
        OnSkillCanceled += HandleCancel;
    }

    private void OnDisable()
    {
        OnSkillCanceled -= HandleCancel;
    }

    private void HandleCancel()
    {
        StopStream();
    }

    public void StopStream()
    {
        if (_isStreaming) PayRemainingEnergy();

        if (_streamCoroutine != null)
        {
            StopCoroutine(_streamCoroutine);
            _streamCoroutine = null;
        }

        CmdDestroyIcyStreamEffect();

        _isStreaming = false;
    }


    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        while (Targeting.GetTempTarget()?.Targetable == null && !_disactive)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget(Targeting.GetMousePoint(), 0.5f);

                var temp = Targeting.GetTempTarget()?.Targetable as Character;

                if (temp != null)
                {
                    Targeting.SetTarget(temp);

                    break;
                }
            }

            yield return null;
        }

        var target = Targeting.GetTarget()?.Character;

        if (target != null)
        {
            targetInfo.AddTarget(target);
            callbackDataSaved(targetInfo);
        }
    }

    protected override IEnumerator CastJob()
    {
        if (!HasEnoughResourcesToStart())
        {
            TryCancel(true);
            yield break;
        }

        _cachedTarget = Targeting.GetTarget()?.Character;
        if (_cachedTarget == null) yield break;

        _isStreaming = true;

        if (!Cost.TryPaySingle(_runeCost, ResourceType.Rune, shouldModify: false))
        {
            TryCancel(true);
            yield break;
        }

        CmdSpawnIcyStreamEffect( _streamStartPoint.gameObject, _cachedTarget.gameObject);

        _streamCoroutine = StartCoroutine(StreamRoutine());

        yield return _streamCoroutine;

        CmdDestroyIcyStreamEffect();
        _isStreaming = false;
    }

    private IEnumerator StreamRoutine()
    {
        for (int tick = 1; tick <= MaxTicks; tick++)
        {
            yield return new WaitForSeconds(_tickInterval);

            if (!IsStreamValid() || _cachedTarget.IsDead)
            {
                TryCancel(true);
                yield break;
            }

            _currentTick = tick;

            CurrentState = new IcyStreamState
            {
                Target = _cachedTarget,
                CurrentTick = tick,
                MaxTicks = MaxTicks
            };

            ApplyTick(tick);
        }
    }

    public bool TryGetState(out IcyStreamState state)
    {
        if (!_isStreaming)
        {
            state = default;
            return false;
        }

        state = CurrentState;
        return true;
    }

    private bool IsStreamValid()
    {
        if (_cachedTarget == null) return false;
        float distance = Vector3.Distance( _cachedTarget.transform.position, transform.position);

        if (distance > AreaInfo.Radius) return false;
        if (!Cost.TryPaySingle(_energyPerTick, ResourceType.Energy, shouldModify: false)) return false;

        return true;
    }

    private void PayRemainingEnergy()
    {
        if (!_isStreaming) return;
        if (_currentTick >= MaxTicks) return;

        int remainingTicks = MaxTicks - _currentTick;
        float totalEnergyToPay = remainingTicks * _energyPerTick;

        if (Hero.TryGetResource(ResourceType.Energy, out var resource)) resource.CmdUse(totalEnergyToPay);
    }

    private void ApplyTick(int tickNumber)
    {
        if (_cachedTarget == null) return;
        if (_cachedTarget.IsDead) return;

        Damage damage = new Damage
        {
            Value = tickNumber,
            Type = Info.DamageType
        };

        CmdApplyDamage(damage, _cachedTarget.gameObject);
        CmdAddCooling(_cachedTarget);
    }

    private void ApplyCoolingWithFrostEnergyBonus(Character target)
    {
        bool hasFrostEnergy = target.CharacterState.CheckForState(States.FrostEnergy);

        int currentStacks = target.CharacterState.CheckStateStacks(States.Cooling);
        int stacksAfterApply = currentStacks + 1;

        if (hasFrostEnergy)
        {
            float bonusDamage = stacksAfterApply * FrostEnergyCoolingBonusPerStack;

            Damage bonus = new Damage
            {
                Value = bonusDamage,
                Type = DamageType.Magical
            };

            target.Health.TryTakeDamage(ref bonus, this);
        }

        target.CharacterState.AddState(States.Cooling, 12f, 0, Hero.gameObject, Name);
    }

    [Command]
    private void CmdSpawnIcyStreamEffect(GameObject startPoint, GameObject targetPoint)
    {
        if (_icyStreamPrefab == null || startPoint == null || targetPoint == null)
            return;

        GameObject effectInstance = Instantiate(_icyStreamPrefab, startPoint.transform.position, Quaternion.identity);

        NetworkServer.Spawn(effectInstance);

        RpcInitEffects(effectInstance, startPoint, targetPoint);

        _activeEffect = effectInstance;
    }

    [Command]
    private void CmdDestroyIcyStreamEffect()
    {
        if (_activeEffect != null)
        {
            NetworkServer.Destroy(_activeEffect);
            _activeEffect = null;
        }
    }

    [Command]
    private void CmdAddCooling(Character character)
    {
        if (character == null) return;

        ApplyCoolingWithFrostEnergyBonus(character);
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

    protected override void ClearData()
    {
        Targeting.ClearTarget();

        if (_streamCoroutine != null)
        {
            StopCoroutine(_streamCoroutine);
            _streamCoroutine = null;
        }
    }
}