using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.Animations;
#endif

[Serializable]
public struct TriggerClipCache
{
    public string Trigger;
    public AnimationClip Clip;
    public float Duration;
}

[Serializable]
public class AnimationComponent : BaseSkillComponent
{
    #region InspectorFields
    [SerializeField] private List<string> _prepareTriggers;
    [SerializeField] private List<string> _castTriggers;
    [SerializeField] private int _castAnimatorLayer = 0;
    
    [Header("Baked cache for int-hash triggers (AnimTriggerCast / AnimTriggerCastDelay)")]
    [SerializeField] private AnimationClip _cachedCastClip;
    [SerializeField] private float _cachedCastClipDuration;
    [SerializeField] private AnimationClip _cachedCastDelayClip;
    [SerializeField] private float _cachedCastDelayClipDuration;

    [Header("Baked cache (fill via Tools > Skills > Cache Animation Trigger Durations)")]
    [SerializeField] private List<TriggerClipCache> _cachedTriggerClips = new();
    #endregion

    #region RuntimeVariables
    private Animator _animator;
    private NetworkAnimator _netAnimator;
    private AnimationClip _activeClip = null;
    private int _activeTrigger = 0;
    private float _activeClipDuration = -1;
    private Dictionary<string, float> _clipDurations = new();           //[clipName] => duration
    private Dictionary<string, AnimationClip> _triggerToClip = new();   //[triggerName] => clip

    public float ActiveClipRawLength => _activeClip != null ? _activeClip.length : -1f;
    #endregion

    #region Properties
    public List<string> PrepareTriggers => _prepareTriggers;
    public List<string> CastTriggers => _castTriggers;
    public float CastSpeed { get => _skill.GetCastSpeed(); }
    public AnimationClip ActiveClip { get => _activeClip; }
    public float ActiveClipDuration { get => _activeClipDuration; }
    #endregion

    #region Methods
    #region Initialization
    public override void Init(Skill skill)
    {
        base.Init(skill);
        _animator = _character.GetComponent<Animator>();
        _netAnimator = _character.GetComponent<NetworkAnimator>();

        LoadBakedCache();
    }
    
    private void LoadBakedCache()
    {
        if (_cachedTriggerClips != null)
        {
            foreach (var entry in _cachedTriggerClips)
            {
                if (string.IsNullOrEmpty(entry.Trigger) || entry.Clip == null) continue;
                _triggerToClip.TryAdd(entry.Trigger, entry.Clip);
                _clipDurations.TryAdd(entry.Clip.name, entry.Duration > 0f ? entry.Duration : entry.Clip.length);
            }
        }

        if (_cachedCastClip != null)
            _clipDurations.TryAdd(_cachedCastClip.name, _cachedCastClipDuration > 0f ? _cachedCastClipDuration : _cachedCastClip.length);

        if (_cachedCastDelayClip != null)
            _clipDurations.TryAdd(_cachedCastDelayClip.name, _cachedCastDelayClipDuration > 0f ? _cachedCastDelayClipDuration : _cachedCastDelayClip.length);
    }

    public AnimationClip GetCachedCastClip() => _cachedCastClip;
    public AnimationClip GetCachedCastDelayClip() => _cachedCastDelayClip;
    #endregion
    
    #if UNITY_EDITOR
public bool CacheIntHashTriggersEditor(Animator animator, int castTriggerHash, int castDelayTriggerHash)
{
    if (animator == null) return false;
    if (animator.runtimeAnimatorController is not AnimatorController controller)
    {
        Debug.LogWarning($"[AnimationComponent] Controller on {animator.gameObject.name} is not an editable AnimatorController.");
        return false;
    }

    bool changed = false;

    if (castTriggerHash != 0)
    {
        var clip = FindClipForHashEditor(controller, castTriggerHash);
        if (clip != null && (clip != _cachedCastClip || !Mathf.Approximately(_cachedCastClipDuration, clip.length)))
        {
            _cachedCastClip = clip;
            _cachedCastClipDuration = clip.length;
            changed = true;
        }
        else if (clip == null)
        {
            Debug.LogError($"[AnimationComponent] Couldn't find clip for AnimTriggerCast hash {castTriggerHash} on {animator.gameObject.name}");
        }
    }

    if (castDelayTriggerHash != 0)
    {
        var clip = FindClipForHashEditor(controller, castDelayTriggerHash);
        if (clip != null && (clip != _cachedCastDelayClip || !Mathf.Approximately(_cachedCastDelayClipDuration, clip.length)))
        {
            _cachedCastDelayClip = clip;
            _cachedCastDelayClipDuration = clip.length;
            changed = true;
        }
        else if (clip == null)
        {
            Debug.LogError($"[AnimationComponent] Couldn't find clip for AnimTriggerCastDelay hash {castDelayTriggerHash} on {animator.gameObject.name}");
        }
    }

    return changed;
}

private static AnimationClip FindClipForHashEditor(AnimatorController controller, int triggerHash)
{
    string paramName = controller.parameters
        .Where(p => p.type == AnimatorControllerParameterType.Trigger)
        .Select(p => p.name)
        .FirstOrDefault(name => Animator.StringToHash(name) == triggerHash);

    if (paramName == null) return null;

    return FindClipForTriggerEditor(controller, paramName);
}
#endif

    #region Playing
    public void PlayTrigger(string triggerName, float castSpeed = float.MinValue)
    {
        if (castSpeed == float.MinValue)
            castSpeed = CastSpeed;

        int hash = Animator.StringToHash(triggerName);
        _animator.SetFloat(HashAnimPlayer.CastSpeed, castSpeed);
        _animator.SetTrigger(hash);
        _netAnimator.SetTrigger(hash);

        _activeTrigger = hash;
        _activeClip = GetAnimationFromTrigger(triggerName);

        if (_activeClip != null)
        {
            _activeClipDuration = GetClipDuration(_activeClip.name);
        }
        else
        {
            _activeClipDuration = -1f;
            _skill.StartCoroutine(ResolveClipAtRuntime(triggerName));
        }
    }

    public void PlayPreparing()
    {
        var anim = GetRandom(_prepareTriggers);
        if (anim == null)
            return;

        PlayTrigger(anim);
    }

    public void PlayCasting()
    {
        var anim = GetRandom(_castTriggers);
        if (anim == null)
            return;

        PlayTrigger(anim);
    }

    public void Cancel()
    {
        ResetCurrentTrigger();

        _animator.SetTrigger(HashAnimPlayer.AnimCancled);
        _netAnimator.SetTrigger(HashAnimPlayer.AnimCancled);
    }

    public void ResetCurrentTrigger()
    {
        if (_activeClip == null)
            return;
        _animator.ResetTrigger(_activeTrigger);
        _netAnimator.ResetTrigger(_activeTrigger);
        _activeClip = null;
        _activeClipDuration = -1;
        _activeTrigger = 0;
    }

    public string GetRandom(List<string> list)
    {
        if (list == null || list.Count == 0)
            return null;

        if (list.Count == 1)
            return list[0];
        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    public void SyncSpeedToRemaining(float clipLength, float remainingDuration, int layer = 0)
    {
        if (clipLength <= 0f || remainingDuration <= 0.0001f) return;

        var stateInfo = _animator.GetCurrentAnimatorStateInfo(layer);
        float normalizedTime = stateInfo.normalizedTime % 1f;
        float remainingClipTime = clipLength * Mathf.Max(0f, 1f - normalizedTime);

        float newSpeed = remainingClipTime / remainingDuration;
        _animator.SetFloat(HashAnimPlayer.CastSpeed, newSpeed);
    }
    #endregion

    #region Caching
    public float GetClipDuration(string clipName)
    {
        if (_clipDurations.TryGetValue(clipName, out float duration))
            return duration / CastSpeed;

        AnimationClip animation = null;
        foreach (AnimationClip anim in _animator.runtimeAnimatorController.animationClips)
        {
            if (anim.name == clipName)
            {
                animation = anim;
                break;
            }
        }

        if (animation == null)
        {
            Debug.LogError($"Couldn't find animation {clipName}", _character.gameObject);
            return -1;
        }

        _clipDurations.TryAdd(clipName, animation.length);
        return animation.length / CastSpeed;
    }
    
    public AnimationClip GetAnimationFromTrigger(string trigger)
    {
        if (_triggerToClip.TryGetValue(trigger, out var cached))
            return cached;

#if UNITY_EDITOR
        return GetAnimationFromTriggerEditor(trigger);
#else
        return null;
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only: сканирует AnimatorController в поисках клипа по имени параметра-триггера.
    /// </summary>
    private AnimationClip GetAnimationFromTriggerEditor(string trigger)
    {
        if (_animator == null) return null;

        if (_animator.runtimeAnimatorController is not AnimatorController controller)
        {
            Debug.LogError("No runtime controller");
            return null;
        }

        var clip = FindClipForTriggerEditor(controller, trigger);
        if (clip != null)
        {
            _triggerToClip.TryAdd(trigger, clip);
            _clipDurations.TryAdd(clip.name, clip.length);
        }
        else
        {
            Debug.LogError($"Couldn't find any transition for trigger {trigger}");
        }

        return clip;
    }

    /// <summary>
    /// Пробегает все переходы AnimatorController (включая AnyState) в поисках условия по имени триггера.
    /// </summary>
    private static AnimationClip FindClipForTriggerEditor(AnimatorController controller, string trigger)
    {
        foreach (var layer in controller.layers)
        {
            var allTransitions = layer.stateMachine.anyStateTransitions
                .Concat(layer.stateMachine.states.SelectMany(s => s.state.transitions));

            foreach (var transition in allTransitions)
            {
                foreach (var condition in transition.conditions)
                {
                    if (condition.parameter == trigger && transition.destinationState.motion is AnimationClip clip)
                        return clip;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Вызывается инструментом кэширования (Tools-меню или контекстное меню SkillManager) —
    /// прогоняет все триггеры этого компонента через указанный Animator и пишет результат
    /// в сериализуемый _cachedTriggerClips
    /// </summary>
    public bool CacheAnimationTriggersEditor(Animator animator)
    {
        if (animator == null) return false;
        if (animator.runtimeAnimatorController is not AnimatorController controller)
        {
            Debug.LogWarning($"[AnimationComponent] Controller on {animator.gameObject.name} is not an editable AnimatorController.");
            return false;
        }

        _cachedTriggerClips ??= new List<TriggerClipCache>();
        bool changed = false;

        var allTriggers = (_prepareTriggers ?? new List<string>())
            .Concat(_castTriggers ?? new List<string>())
            .Where(t => !string.IsNullOrEmpty(t))
            .Distinct();

        foreach (var trigger in allTriggers)
        {
            Debug.LogError($"[AnimationComponent] trigger name {trigger}");
            var clip = FindClipForTriggerEditor(controller, trigger);
            if (clip == null)
            {
                Debug.LogError($"[AnimationComponent] Couldn't find clip for trigger '{trigger}' on {animator.gameObject.name}");
                continue;
            }

            int existingIndex = _cachedTriggerClips.FindIndex(c => c.Trigger == trigger);
            var entry = new TriggerClipCache { Trigger = trigger, Clip = clip, Duration = clip.length };

            if (existingIndex >= 0)
            {
                var existing = _cachedTriggerClips[existingIndex];
                if (existing.Clip != clip || !Mathf.Approximately(existing.Duration, clip.length))
                {
                    _cachedTriggerClips[existingIndex] = entry;
                    changed = true;
                }
            }
            else
            {
                _cachedTriggerClips.Add(entry);
                changed = true;
            }
        }

        return changed;
    }
#endif
    
    private IEnumerator ResolveClipAtRuntime(string triggerName)
    {
        int initialStateHash = _animator.GetCurrentAnimatorStateInfo(_castAnimatorLayer).fullPathHash;
        const float timeout = 1f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            yield return null;
            elapsed += Time.deltaTime;

            bool stateChanged = _animator.GetCurrentAnimatorStateInfo(_castAnimatorLayer).fullPathHash != initialStateHash;
            if (!stateChanged) continue;
            if (_animator.IsInTransition(_castAnimatorLayer)) continue;

            var clip = GetHeaviestClip(_castAnimatorLayer);
            if (clip != null)
            {
                CacheRuntimeResolvedClip(triggerName, clip);
                yield break;
            }
        }

        Debug.LogWarning(
            $"[AnimationComponent] Could not resolve clip for trigger '{triggerName}' at runtime within {timeout}s. " +
            $"Consider running the animation trigger cache tool for this prefab.",
            _character != null ? _character.gameObject : null);
    }

    private AnimationClip GetHeaviestClip(int layer)
    {
        var clipInfos = _animator.GetCurrentAnimatorClipInfo(layer);
        if (clipInfos.Length == 0) return null;

        var best = clipInfos[0];
        for (int i = 1; i < clipInfos.Length; i++)
            if (clipInfos[i].weight > best.weight) best = clipInfos[i];

        return best.clip;
    }

    private void CacheRuntimeResolvedClip(string triggerName, AnimationClip clip)
    {
        _triggerToClip[triggerName] = clip;
        _clipDurations[clip.name] = clip.length;

        if (_activeTrigger == Animator.StringToHash(triggerName))
        {
            _activeClip = clip;
            _activeClipDuration = GetClipDuration(clip.name);
        }
    }
    #endregion
    #endregion
}