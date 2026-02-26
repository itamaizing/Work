using System;
using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
public class FlowOfLight : Skill
{
    [Header("Flow Light Settings")]
    [SerializeField] private float buffDuration = 18f;
    [SerializeField] private GameObject effectPrefabLight;
    [SerializeField] private AbilityInfo lightInfo;

    [Header("Flow Dark Settings")]
    [SerializeField] private float debuffDuration = 18f;
    [SerializeField] private GameObject effectPrefabDark;
    [SerializeField] private AbilityInfo darkInfo;

    [SerializeField] private StunMagicPassiveSkill stunMagicPassiveSkill;

    [SyncVar(hook = nameof(OnModeChanged))] public bool isLightMode = true;
    public event Action OnModeChange;

    private GameObject _activeEffect;

    private bool IsAllyTarget(Character target) => target != null && target.gameObject.layer == LayerMask.NameToLayer("Allies");
    private bool IsEnemyTarget(Character target) => target != null && target.gameObject.layer == LayerMask.NameToLayer("Enemy");

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("FlowSpellStart");

    #region Talent
    private bool _spiritEnergyAddTalent;
    public void SpiritEnergyAddTalent(bool value) => _spiritEnergyAddTalent = value;
    #endregion

    protected override bool IsCanCast =>
		Targeting.GetTarget()?.Character != null &&
        Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position) <= AreaInfo.Radius &&
        Targeting.NoObstacles(Targeting.GetTarget().Character.transform.position, transform.position, _obstacle) &&
        ((isLightMode && IsAllyTarget(Targeting.GetTarget()?.Character)) || (!isLightMode && IsEnemyTarget(Targeting.GetTarget()?.Character)));

    private void OnEnable()
    {
        OnModeChange += UpdateMode;
        OnSkillCanceled += HandleSkillCanceled;
        UpdateMode();
    }

    private void OnDisable()
    {
        OnModeChange -= UpdateMode;
        OnSkillCanceled -= HandleSkillCanceled;
    }

    public void FlowLightCast() => AnimStartCastCoroutine();
    public void FlowLightthEnd() => AnimCastEnded();

    public void MoveFlowLight()
    {
        _hero.Move.SetCanMove(false);
        _hero.Move.StopMoveAndAnimationMove();
    }

    public void SwitchMode()
    {
        CmdSwitchMode();
    }

    private void HandleSkillCanceled()
    {
        if (_hero != null && _hero.Move != null)
        {
            Hero.Move.SetCanMove(true);
        }
    }

    private void OnModeChanged(bool oldValue, bool newValue)
    {
        //UpdateMode();
        OnModeChange?.Invoke();
    }

    private void UpdateMode()
    {
        School = isLightMode ? Schools.Light : Schools.Dark;
        AbilityInfoHero = isLightMode ? lightInfo : darkInfo;
    }

    private void ApplySpiritBuff(Character target)
    {
        if (!_spiritEnergyAddTalent || target == null) return;

        var stateComponent = target.GetComponent<CharacterState>();
        if (stateComponent == null) return;

        if (isLightMode) CmdStateSpiritEnergyOrHealth(stateComponent, States.SpiritEnergy, buffDuration);
        else CmdStateSpiritEnergyOrHealth(stateComponent, States.SpiritHealth, debuffDuration);
    }

    private void TryApplyExtraState(Character target)
    {
        if (!stunMagicPassiveSkill.IsFillingDestruction || target == null) return;

        var stateComponent = target.GetComponent<CharacterState>();
        if (stateComponent == null) return;

        if (!isLightMode && UnityEngine.Random.value <= 0.2f) CmdStateRestorationOrDestruction(stateComponent, States.Destruction, 12f);
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        while (Targeting.GetTempTarget()?.Character == null)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget();
            }
            yield return null;
        }
        Targeting.SetTarget(Targeting.GetTempTarget()?.Character);
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Targeting.GetTarget()?.Character);
        targetDataSavedCallback(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (Targeting.GetTarget()?.Character == null || !IsCanCast)
        {
            TryCancel();
            yield break;
        }

        TryPayCost();
        CmdSpawnEffect(gameObject, Targeting.GetTarget()?.Character.gameObject);

        float elapsed = 0f;
        float interval = 1f;
        float tickValue = 8f;

        var manaResource = Hero.TryGetResource(ResourceType.Mana);
        Vector3 initialPosition = transform.position;
        float maxMoveDistance = 0.5f;

        while (elapsed < CastStreamDuration)
        {
            if (Targeting.GetTarget().Character == null || !Targeting.GetTarget().Character.gameObject.activeSelf ||
                Input.GetMouseButtonDown(1) ||
                Vector3.Distance(transform.position, Targeting.GetTarget().Character.transform.position) > AreaInfo.Radius ||
                Vector3.Distance(transform.position, initialPosition) > maxMoveDistance ||
                (manaResource != null && manaResource.CurrentValue < 1f))
            {

                _hero.Animator.ResetTrigger(AnimTriggerCast);
                _hero.NetworkAnimator.ResetTrigger(AnimTriggerCast);

                CmdCrossFade();
                _hero.Animator.CrossFade("FlowSpellEnd", 0.1f);

                TryCancel();
                CmdDestroyEffect();
                yield break;
            }

            if (elapsed % interval < Time.deltaTime)
            {
                if (isLightMode && IsAllyTarget(Targeting.GetTarget()?.Character))
                {
                    Heal heal = new Heal { Value = tickValue };
                    CmdApplyHeal(heal, Targeting.GetTarget()?.Character.gameObject, this, Name);
                    TryApplyExtraState(Targeting.GetTarget()?.Character);
                    ApplySpiritBuff(Targeting.GetTarget()?.Character);
                }
                else if (!isLightMode && IsEnemyTarget(Targeting.GetTarget()?.Character))
                {
                    Damage damage = new Damage
                    {
                        Value = tickValue,
                        Type = DamageType,
                        School = School
                    };
                    CmdApplyDamage(damage, Targeting.GetTarget()?.Character.gameObject);
                    TryApplyExtraState(Targeting.GetTarget()?.Character);
                    ApplySpiritBuff(Targeting.GetTarget()?.Character);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        _hero.Animator.ResetTrigger(AnimTriggerCast);
        _hero.NetworkAnimator.ResetTrigger(AnimTriggerCast);

        CmdCrossFade();
        _hero.Animator.CrossFade("FlowSpellEnd", 0.1f);
        CmdDestroyEffect();
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
        _hero.Move.StopLookAt();
        CmdDestroyEffect();
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0)
            Targeting.SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
    }

    [Command] private void CmdCrossFade() => _hero.Animator.CrossFade("FlowSpellEnd", 0.1f);

    [Command]
    private void CmdSwitchMode()
    {
        UpdateMode();
        isLightMode = !isLightMode;
    }

    [Command]
    private void CmdSpawnEffect(GameObject start, GameObject end)
    {
        if (effectPrefabDark == null || effectPrefabLight == null || start == null || end == null) return;

        GameObject effectInstance = null;

        if (!isLightMode) effectInstance = Instantiate(effectPrefabDark, start.transform.position, Quaternion.identity);
        else effectInstance = Instantiate(effectPrefabLight, start.transform.position, Quaternion.identity);

        SceneManager.MoveGameObjectToScene(effectInstance, _hero.NetworkSettings.MyRoom);
        NetworkServer.Spawn(effectInstance);

        _activeEffect = effectInstance;

        RpcInitEffect(effectInstance, start, end);
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

    [Command]
private void CmdStateRestorationOrDestruction(NetworkIdentity targetNetIdentity, States states, float duration)
{
    if (targetNetIdentity == null) return;

    var stateComponent = targetNetIdentity.GetComponent<CharacterState>();
    if (stateComponent == null) return;

    stateComponent.AddState(states, duration, 0, gameObject, Name);
}
    [Command] private void CmdStateRestorationOrDestruction(CharacterState stateComponent, States states, float duration) => ClientRpcStateRestorationOrDestruction(stateComponent, states, duration);
    [Command] private void CmdStateSpiritEnergyOrHealth(CharacterState stateComponent, States states, float duration) => ClientRpcSpiritEnergyOrHealth(stateComponent, states, duration);

    [ClientRpc] private void ClientRpcSpiritEnergyOrHealth(CharacterState stateComponent, States states, float duration) { stateComponent.AddStateLogic(states, duration, 1f, Schools.None, gameObject, Name); }
    [ClientRpc] private void ClientRpcStateRestorationOrDestruction(CharacterState stateComponent, States states, float duration) { stateComponent.AddStateLogic(states, duration, 0, Schools.None, gameObject, "FlowOfLight"); }


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
}
