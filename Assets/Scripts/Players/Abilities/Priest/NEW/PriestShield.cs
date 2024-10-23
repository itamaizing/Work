using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PriestShield : Skill
{
    [Header("Shield (Light Mode) Settings")]
    [SerializeField] private float lightShieldDuration = 18f;
    [SerializeField] private float tiredSoulDuration = 12f;
    [SerializeField] private float selfCastTime = 0.6f;
    [SerializeField] private float allyCastTime = 1.2f;
    [SerializeField] private float absorbAmount = 20f;
    [SerializeField] private List<SkillEnergyCost> manaCostLight;
    [SerializeField] private float cooldownLight = 4f;
    
    [Header("Shield (Dark Mode) Settings")]
    [SerializeField] private float darkShieldDuration = 12f;
    [SerializeField] private float damageDebuffDelay = 0.2f;
    [SerializeField] private float maxDamagePerTick = 20f;
    [SerializeField] private List<SkillEnergyCost> manaCostDark;
    [SerializeField] private float cooldownDark = 4f;
    [SerializeField] private float darkCastTime = 1.2f;

    public bool isLightMode = true;
    
    private float _nextAvailableTime;
    private Character _target;

    protected override bool IsCanCast => IsCanCastCheck();

    protected override int AnimTriggerCastDelay => throw new NotImplementedException();

    protected override int AnimTriggerCast => throw new NotImplementedException();

    private bool IsCanCastCheck()
    {
        if (_target == null || Time.time < _nextAvailableTime) return false;
        
        return Vector3.Distance(transform.position, _target.transform.position) <= Radius;
    }

    public event Action OnModeChange;

    private void OnEnable()
    {
        OnModeChange += HandleModeChange;
        UpdateMode();
    }

    private void OnDisable()
    {
        OnModeChange -= HandleModeChange;
    }

    public void SwitchMode()
    {
        isLightMode = !isLightMode;
        OnModeChange?.Invoke();
    }

    private void HandleModeChange()
    {
        UpdateMode();
    }

    private void UpdateMode()
    {
        CastDeley = isLightMode ? allyCastTime : darkCastTime;
        CooldownTime = isLightMode ? cooldownLight : cooldownDark;
        School = isLightMode ? Schools.Light : Schools.Dark;
        TargetsLayers = isLightMode ? LayerMask.GetMask("Allies") : LayerMask.GetMask("Enemy");
    }

    protected override IEnumerator PrepareJob()
    {
        while (_target == null)
        {
            if (Input.GetMouseButton(0))
            {
                _target = GetRaycastTarget(true);
                
                if (_target == transform.GetComponentInParent<Character>())
                {
                    CastDeley = selfCastTime;
                }
            }
            yield return null;
        }
    }

    protected override IEnumerator CastJob()
    {
        if (_target == null || !IsCanCast) yield break;

        _nextAvailableTime = Time.time + CooldownTime;
        
        if (isLightMode)
        {
            HandleLightShield();
        }
        else
        {
            HandleDarkShield();
        }

        yield return null;
    }

    private void HandleLightShield()
    {
        if (_target == null) return;

        var characterState = _target.GetComponent<CharacterState>();
        
        if (characterState.CheckForState(States.TiredSoul))
        {
            Debug.Log("Cannot apply Light Shield. Target has 'TiredSoul' debuff.");
            return;
        }

        if (TryPayCost(manaCostLight))
        {
            //characterState.CmdAddState(States.TiredSoul, tiredSoulDuration, 0, _target.gameObject, "TiredSoul");
            characterState.CmdAddState(States.LightShield, lightShieldDuration, absorbAmount, _target.gameObject, "LightShield");
        
            Debug.Log("Light Shield applied to " + _target.name);
        }
    }

    private void HandleDarkShield()
    {
        if (_target == null) return;

        var characterState = _target.GetComponent<CharacterState>();
        if (TryPayCost(manaCostDark))
        {
            characterState.CmdAddState(States.DarkShield, darkShieldDuration, maxDamagePerTick, _target.gameObject, "DarkShield");
        }
    }
    
    protected override void ClearData()
    {
        _target = null;
    }
}