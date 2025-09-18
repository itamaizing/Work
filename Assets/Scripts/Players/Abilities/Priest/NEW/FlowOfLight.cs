using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FlowOfLight : Skill
{
    [Header("Flow Settings")]
    [SerializeField] private GameObject _effectPrefab;
    [SerializeField] private Transform _spawnPoint;

    [Header("Ability Info")]
    [SerializeField] private AbilityInfo lightInfo;
    [SerializeField] private AbilityInfo darkInfo;

    [SyncVar(hook = nameof(OnModeChanged))] public bool isLightMode = true;
    public event System.Action OnModeChange;

    private GameObject _activeEffect;
    private Character _target;

    private bool IsAllyTarget(Character target) => target != null && target.gameObject.layer == LayerMask.NameToLayer("Allies");
    private bool IsEnemyTarget(Character target) => target != null && target.gameObject.layer == LayerMask.NameToLayer("Enemy");

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    protected override bool IsCanCast =>
        _target != null &&
        Vector3.Distance(_target.transform.position, transform.position) <= Radius &&
        NoObstacles(_target.transform.position, transform.position, _obstacle) &&
        ((isLightMode && IsAllyTarget(_target)) || (!isLightMode && IsEnemyTarget(_target)));

    private void OnEnable()
    {
        OnModeChange += UpdateMode;
        UpdateMode();
    }

    private void OnDisable()
    {
        OnModeChange -= UpdateMode;
    }

    public void SwitchMode()
    {
        CmdSwitchMode();
    }

    private void OnModeChanged(bool oldValue, bool newValue)
    {
        UpdateMode();
        OnModeChange?.Invoke();
    }

    private void UpdateMode()
    {
        School = isLightMode ? Schools.Light : Schools.Dark;
        AbilityInfoHero = isLightMode ? lightInfo : darkInfo;
    }

    protected override IEnumerator PrepareJob(System.Action<TargetInfo> callbackDataSaved)
    {
        while (_target == null)
        {
            if (GetMouseButton)
            {
                var candidate = GetRaycastTarget();
                if (candidate != null)
                {
                    if ((isLightMode && IsAllyTarget(candidate)) || (!isLightMode && IsEnemyTarget(candidate)))
                    {
                        _target = candidate;
                        _target.SelectedCircle.IsActive = true;
                    }
                }
            }
            yield return null;
        }

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Targets.Add(_target);
        callbackDataSaved(targetInfo);
    }


    protected override IEnumerator CastJob()
    {
        if (_target == null || !IsCanCast)
        {
            TryCancel();
            yield break;
        }

        TryPayCost();
        CmdSpawnEffect(gameObject, _target.gameObject);

        float elapsed = 0f;
        float interval = 1f;
        float tickValue = 8f;

        var manaResource = Hero.TryGetResource(ResourceType.Mana);
        Vector3 initialPosition = transform.position;
        float maxMoveDistance = 0.5f;

        while (elapsed < CastStreamDuration)
        {
            if (_target == null || !_target.gameObject.activeSelf ||
                Input.GetMouseButtonDown(1) ||
                Vector3.Distance(transform.position, _target.transform.position) > Radius ||
                Vector3.Distance(transform.position, initialPosition) > maxMoveDistance ||
                (manaResource != null && manaResource.CurrentValue < 1f))
            {
                TryCancel();
                CmdDestroyEffect();
                yield break;
            }

            if (elapsed % interval < Time.deltaTime)
            {
                if (isLightMode && IsAllyTarget(_target))
                {
                    Heal heal = new Heal { Value = tickValue };
                    CmdApplyHeal(heal, _target.gameObject, this, Name);
                }
                else if (!isLightMode && IsEnemyTarget(_target))
                {
                    Damage damage = new Damage
                    {
                        Value = tickValue,
                        Type = DamageType,
                        School = School
                    };
                    CmdApplyDamage(damage, _target.gameObject);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        CmdDestroyEffect();
    }

    protected override void ClearData()
    {
        _target = null;
        _hero.Move.StopLookAt();
        CmdDestroyEffect();
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Targets.Count > 0)
            _target = (Character)targetInfo.Targets[0];
    }

    [Command]
    private void CmdSwitchMode()
    {
        isLightMode = !isLightMode;
        UpdateMode();
    }

    [Command]
    private void CmdSpawnEffect(GameObject start, GameObject end)
    {
        if (_effectPrefab == null || start == null || end == null) return;

        GameObject effectInstance = Instantiate(_effectPrefab, start.transform.position, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(effectInstance, _hero.NetworkSettings.MyRoom);
        NetworkServer.Spawn(effectInstance);

        _activeEffect = effectInstance;

        RpcInitEffect(effectInstance, start, end);
    }

    [ClientRpc]
    private void RpcInitEffect(GameObject effect, GameObject start, GameObject end)
    {
        if (effect == null) return;

        FlowLightEffect[] flows = effect.GetComponentsInChildren<FlowLightEffect>(true);
        foreach (var flow in flows)
        {
            flow.Initialize(start, end);
            flow.Activate();
        }

        if (flows.Length == 0)
        {
            Debug.LogWarning("FlowLightEffect не найден ни на одном дочернем объекте эффекта: " + effect.name);
        }
    }

    [Command]
    private void CmdDestroyEffect()
    {
        if (_activeEffect != null)
        {
            NetworkServer.Destroy(_activeEffect);
            _activeEffect = null;
        }
    }
}