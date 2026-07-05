using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BlindnessState : RefreshingState
{
    public bool turnOff = false;

    private float _baseDuration;
    private VolumeProfile _volumeProfile;
    private Bloom _bloom;
    private Vignette _vignette;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Ability };

    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State => States.Blind;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        //  Debug.Log($"Entering Blindness State on Character netId: {character.netId}");
        _baseDuration = durationToExit;
        characterState = character;
        MaxStacksCount = 1;
        currentStacksCount = 1;

        if (characterState.isOwned) ApplyEffectToLocalCamera();

        if (character.TryGetComponent<Character>(out var ability))
        {
            abilities = ability.Abilities;
            foreach (var abil in abilities.Abilities)
                if (abil.Info.SkillType == SkillType.Target)
                    abil.Disactive = true;
        }
    }


    public override void UpdateState()
    {
    }

    protected override void ExitState()
    {
        if (characterState.isOwned) RemoveEffectFromLocalCamera();

        if (characterState.TryGetComponent<Character>(out var ability))
        {
            abilities = ability.Abilities;
            foreach (var abil in abilities.Abilities) if (abil.Info.SkillType == SkillType.Target) abil.Disactive = false;
        }
    }

    public override bool Stack(float time)
    {
        duration += time;
        RemainingDuration = duration;
        return false;
    }

    private void ApplyEffectToLocalCamera()
    {
       // Debug.Log("Applying Blindness effect to local camera...");
        var camera = Camera.main;
        if (camera == null)
        {
            Debug.LogError("No camera found for the local client.");
            return;
        }

        var volume = camera.GetComponent<Volume>();
        if (volume != null && volume.profile != null)
        {
            _volumeProfile = volume.profile;

            if (_volumeProfile.TryGet(out _bloom) && _volumeProfile.TryGet(out _vignette)) EnableBlindnessEffect();
            else Debug.LogError("Bloom or Vignette not found in VolumeProfile.");
        }

        else Debug.LogError("Volume component or profile not found on camera.");
    }

    private void RemoveEffectFromLocalCamera()
    {
        if (_bloom != null) _bloom.intensity.overrideState = false;

        if (_vignette != null)
        {
            _vignette.intensity.overrideState = false;
            _vignette.smoothness.overrideState = false;
        }
    }

    private void EnableBlindnessEffect()
    {
        if (_bloom != null)
        {
            _bloom.intensity.overrideState = true;
            _bloom.intensity.value = 10f;
        }

        if (_vignette != null)
        {
            _vignette.intensity.overrideState = true;
            _vignette.intensity.value = 1f;
            _vignette.smoothness.overrideState = true;
            _vignette.smoothness.value = 1f;
        }
    }
}