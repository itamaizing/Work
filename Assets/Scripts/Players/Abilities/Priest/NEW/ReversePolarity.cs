using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class ReversePolarity : Skill
{
    [SerializeField] private List<Skill> _switchableSkills;

    [SerializeField] private AudioClip audioClip;

    protected override int AnimTriggerCastDelay => Animator.StringToHash("Cast");
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => true;

    private AudioSource _audioSource;

    private float _cooldownAfterDarkMode = 6f;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        /*sparkOfLight.CastEnded += RemoveReversePolarityEffect;
        flashOfLight.CastEnded += RemoveReversePolarityEffect;
        restoration.CastEnded += RemoveReversePolarityEffect;
        priestShield.CastEnded += RemoveReversePolarityEffect;
        
        sparkOfLight.CastEnded += SwitchSpells;
        flashOfLight.CastEnded += SwitchSpells;
        restoration.CastEnded += SwitchSpells;
        priestShield.CastEnded += SwitchSpells;
        */
    }

    private void OnDisable()
    {

    }
    public override void LoadTargetData(TargetInfo targetInfo)
    {
        //Debug.LogError("DataError");
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        if (_hero == null) yield break;
        Targeting.SetTarget(_hero);
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(_hero);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (Hero == null || Hero.CharacterState == null || !IsCanCast) yield break;

        //if (!TryPayCost()) yield break;

        CmdPlayShootSound();

        //yield return new WaitForSeconds(CastDeley);

        SwitchSpells();

        if (Hero.CharacterState.CheckForState(States.ReversePolarity))
        {
            RemoveReversePolarityEffect();
        }
        else
        {
            ApplyReversePolarityEffect();
        }
    }

    private void ApplyReversePolarityEffect()
    {
        CmdAddBaff(States.ReversePolarity, -1f, 0, transform.gameObject, Name);
    }

    public void RemoveReversePolarityEffect()
    {
        CmdRemoveBuff(States.ReversePolarity, Hero.gameObject);
    }

    [Command]
    private void CmdAddBaff(States darkState, float duration, float damagePerTick, GameObject target, string skillName)
    {
        var characterState = target.GetComponent<CharacterState>();
        characterState.AddState(darkState, duration, damagePerTick, target, skillName);
    }

    [Command]
    private void CmdRemoveBuff(States state, GameObject target)
    {
        var characterState = target.GetComponent<CharacterState>();
        characterState.RemoveState(state);
    }

    [Command]
    private void CmdPlayShootSound()
    {
        RpcPlayShotSound();
    }

    [ClientRpc]
    private void RpcPlayShotSound()
    {
        if (_audioSource != null && audioClip != null) _audioSource.PlayOneShot(audioClip);
    }

    public void SwitchSpells()
    {
        if (!IsAutoMode)
        {
            _hero.Abilities.AutoSkillCast.DeleteSkill();
            foreach (var switchable in _switchableSkills)
            {
                _hero.Abilities.SkillQueue.RemoveNeededSkillFromQueue(switchable);
            }
        }

        foreach (var skill in _switchableSkills)
        {
            var switchable = (IPolaritySwitchable)skill;
            switchable?.SwitchMode();
        }
    }

    public void SetCooldownFromSpell()
    {
        IncreaseSetCooldown(_cooldownAfterDarkMode);
    }

    protected override void ClearData()
    {
    }
}
