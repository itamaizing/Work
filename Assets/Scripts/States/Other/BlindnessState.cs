using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BlindnessStateStacking : StateStackingRefreshing
{
    public bool turnOff = false;

    private UniversalAdditionalCameraData _cameraData;
    private bool _wasPostProcessingEnabled;
    private Bloom _bloom;
    private Vignette _vignette;
    private (float value, bool overridden) _originalBloomIntensity;
    private (float value, bool overridden) _originalVignetteIntensity;
    private (float value, bool overridden) _originalVignetteSmoothness;
    private readonly List<Skill> _disabledSkills = new();
    private readonly List<StatusEffect> _effects = new() { StatusEffect.Ability };

    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override States State => States.Blind;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        MaxDuration = durationToExit;
        RemainingDuration = durationToExit;

        if (character.isOwned) ApplyEffectToLocalCamera();

        foreach (var skill in abilities.Abilities)
        {
            if (skill.Targeting.SkillType != TargetingType.Target || skill.Disactive) continue;

            skill.Disactive = true;
            if (skill.Disactive) _disabledSkills.Add(skill);
        }
    }

    public override void Reapply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        MaxDuration = durationToExit;
        RemainingDuration = durationToExit;
    }

    public override void UpdateState()
    {
        if (turnOff) ExitState();
    }

    protected override void OnExit()
    {
        RemoveEffectFromLocalCamera();

        foreach (var skill in _disabledSkills)
            if (skill != null) skill.Disactive = false;

        _disabledSkills.Clear();
        CurrentStacksCount = 0;
        turnOff = false;
    }

    private void ApplyEffectToLocalCamera()
    {
        var camera = Camera.main;
        if (camera == null || !camera.TryGetComponent<Volume>(out var volume)) return;

        var profile = volume.profile;
        if (profile == null || !profile.TryGet(out _vignette)) return;

        if (camera.TryGetComponent(out _cameraData))
        {
            _wasPostProcessingEnabled = _cameraData.renderPostProcessing;
            _cameraData.renderPostProcessing = true;
        }

        if (profile.TryGet(out _bloom))
            _originalBloomIntensity = SetOverride(_bloom.intensity, 10f);

        _originalVignetteIntensity = SetOverride(_vignette.intensity, 1f);
        _originalVignetteSmoothness = SetOverride(_vignette.smoothness, 1f);
    }

    private void RemoveEffectFromLocalCamera()
    {
        if (_bloom != null) RestoreOverride(_bloom.intensity, _originalBloomIntensity);

        if (_vignette != null)
        {
            RestoreOverride(_vignette.intensity, _originalVignetteIntensity);
            RestoreOverride(_vignette.smoothness, _originalVignetteSmoothness);
        }

        if (_cameraData != null) _cameraData.renderPostProcessing = _wasPostProcessingEnabled;

        _bloom = null;
        _vignette = null;
        _cameraData = null;
    }

    private static (float value, bool overridden) SetOverride(VolumeParameter<float> parameter, float value)
    {
        var previous = (parameter.value, parameter.overrideState);
        parameter.Override(value);
        return previous;
    }

    private static void RestoreOverride(VolumeParameter<float> parameter, (float value, bool overridden) previous)
    {
        parameter.value = previous.value;
        parameter.overrideState = previous.overridden;
    }
}
