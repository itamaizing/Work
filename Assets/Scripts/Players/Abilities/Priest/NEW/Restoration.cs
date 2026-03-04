using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class Restoration : Skill,IPolaritySwitchable
{
    [Header("Restoration (Light Mode) Settings")]
    [SerializeField] private float healPerTick = 6f;
    [SerializeField] private float lightRange = 4f;
    [SerializeField] private float lightDuration = 12.1f;
    [SerializeField] private float healInterval = 4f;
    [SerializeField] private float lightCastTime = 1.2f;
    [SerializeField] private float effectivenessIncreasePerHeal = 0.1f;
    [SerializeField] private AbilityInfo lightInfo;

    [Header("Restoration (Dark Mode) Settings")]
    [SerializeField] private float damagePerTick = 6f;
    [SerializeField] private float darkRange = 6f;
    [SerializeField] private float darkDuration = 12.1f;
    [SerializeField] private float damageInterval = 3f;
    [SerializeField] private float darkCastTime = 1.2f;
    [SerializeField] private AbilityInfo darkInfo;

    [SerializeField] private AudioClip audioClip;
    
    private float _clickRadius = 0.5f;
    private AudioSource _audioSource;
    private float _accumulatedEffectiveness = 1f;
    private float _totalHealedInInterval = 0f;
    private bool _spiritEnergyTalent;

    private string _initialRestorationName = "Restoration";
    //private IDamageable _target;
    //private Character characterTarget;

    public IDamageable Target => Targeting.GetTarget()?.Character;

    [SyncVar(hook = nameof(OnModeChanged))] public bool isLightMode = true;

    private bool IsAllyTarget(Character target) => target != null && target.gameObject.layer == LayerMask.NameToLayer("Allies");
    private bool IsEnemyTarget(Character target) => target != null && target.gameObject.layer == LayerMask.NameToLayer("Enemy");

    protected override int AnimTriggerCastDelay => Animator.StringToHash("Cast");
    protected override int AnimTriggerCast => 0;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    public event Action OnModeChange;

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

    [Command]
    private void CmdSwitchMode()
    {
        UpdateMode();
        isLightMode = !isLightMode;
    }

    private void OnModeChanged(bool oldValue, bool newValue)
    {
        UpdateMode();
        OnModeChange?.Invoke();
    }

    public void SpiritEnergyTalentActive(bool value)
    {
        _spiritEnergyTalent = value;
    }

    private void UpdateMode()
    {
        AreaInfo.Radius = isLightMode ? lightRange : darkRange;
        Info.School = isLightMode ? Schools.Light : Schools.Dark;
        CastDeley = isLightMode ? lightCastTime : darkCastTime;
        AbilityInfoHero = isLightMode ? lightInfo : darkInfo;
        Targeting.Layer = isLightMode ? LayerMask.GetMask("Allies") : LayerMask.GetMask("Enemy");
        Hero.Abilities.SkillPanelUpdate();
    }

    private void HandleRestorationLight()
    {
        if (Targeting.GetTarget()?.Character == null) return;
        bool isAlly = Targeting.GetTarget()?.Character.gameObject.layer == LayerMask.NameToLayer("Allies");
        if (isAlly && TryPayCost())
        {
            CmdRemoveState(Targeting.GetTarget()?.Character, States.Restoration);
            CmdAddState(Targeting.GetTarget()?.Character, States.Restoration, lightDuration);
        }
    }

    private float GetSpiritEnergyBonus(Character target)
    {
        var characterState = target?.GetComponent<CharacterState>();
        if (characterState == null) return 0f;

        var spiritEnergyState = characterState.GetState(States.SpiritEnergy) as SpiritEnergyState;
        return spiritEnergyState != null ? spiritEnergyState.GetHealBonus() : 0f;
    }

    private void HandleRestorationDark()
    {
        if (Targeting.GetTarget()?.Character == null) return;
        bool isEnemy = Targeting.GetTarget()?.Character.gameObject.layer == LayerMask.NameToLayer("Enemy");
        if (isEnemy && TryPayCost())
        {
            CmdRemoveState(Targeting.GetTarget()?.Character, States.Destruction);
            CmdAddState(Targeting.GetTarget()?.Character, States.Destruction, darkDuration);
        }
    }

    private void OnHealTaken(float healedAmount, Skill skill, string sourceName)
    {
        _totalHealedInInterval += healedAmount;
    }


    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (Targeting.GetTempTarget()?.Character == null)
        {
            if (GetMouseButton)
            {
                Vector3 clickPoint = Targeting.GetMousePoint();

                Targeting.FindTempTarget(clickPoint, _clickRadius, canTargetSelf: true);
                
                if (Targeting.GetTempTarget()?.Character is Character character)
                {
                    if (Targeting.GetTempTarget()?.Character != null && (IsEnemyTarget(character) && isLightMode) || (IsAllyTarget(character) && !isLightMode))
                    {
                        Targeting.ClearTempTarget();
                    }
                    else
                    {
                        Targeting.GetTempTarget().Character.SelectedCircle.IsActive = true;
                        _hero.Move.LookAtTransform(Targeting.GetTempTarget()?.Character.transform);
                    }
                }
            }
            yield return null;
        }

        TargetInfo targetInfo = new();
        Targeting.SetTarget(Targeting.GetTempTarget()?.Character);
        targetInfo.AddTarget(Targeting.GetTarget()?.Character);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (Targeting.GetTarget()?.Character == null) yield break;

        CmdPlayShootSound();

        if (isLightMode)
        {
            HandleRestorationLight();
        }
        else
        {
            HandleRestorationDark();
        }

        yield return null;
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
       // _target = null;
    }

    //[Command]
    private void CmdPlayShootSound()
    {
        RpcPlayShotSound();
    }

    //[Command]
    private void CmdRemoveState(Character character, States states) => character.CharacterState.RemoveState(states);

    
    //[Command]
    private void CmdAddState(Character character, States states, float duration) => character.CharacterState.AddState(states, duration, 0, Hero.gameObject, _initialRestorationName);

    //[ClientRpc]
    private void RpcPlayShotSound()
    {
        if (_audioSource != null && audioClip != null) _audioSource.PlayOneShot(audioClip);
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        Targeting.SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
    }
} 
