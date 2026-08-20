using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor.Animations;
#endif
using UnityEngine;

[Serializable]
public class AnimationComponent : BaseSkillComponent
{
    #region InspectorFields
    //[SerializeField] private bool usePrepareAnimation;
    [SerializeField] private List<string> _prepareTriggers;
    //[SerializeField] private bool useCastAnimation;
    [SerializeField] private List<string> _castTriggers;
    #endregion

    #region RuntimeVariables
    private Animator _animator;
    private NetworkAnimator _netAnimator;
    private AnimationClip _activeClip = null;
    private int _activeTrigger = 0;
    private float _activeClipDuration = -1;
    private Dictionary<string, float> _clipDurations = new();           //[clipName] => duration
    private Dictionary<string, AnimationClip> _triggerToClip = new();   //[triggerName] => clip
    #endregion

    #region Properties
    public List<string> PrepareTriggers => _prepareTriggers;
    public List<string> CastTriggers => _castTriggers;
    public float CastSpeed { get => _skill.GetCastSpeed(); }
    public AnimationClip ActiveClip { get =>  _activeClip; }
    public float ActiveClipDuration { get => _activeClipDuration; }
    #endregion

    #region Methods
    #region Initialization
    public override void Init(Skill skill)
    {
        base.Init(skill);
        _animator = _character.GetComponent<Animator>();
        _netAnimator = _character.GetComponent<NetworkAnimator>();

        CashTriggers();
    }

    public void CashTriggers()
    {
        foreach (string trigger in _prepareTriggers)
        {
            var clip = GetAnimationFromTrigger(trigger);
            if (clip != null)
            {
                _clipDurations.TryAdd(clip.name, clip.length);
            }
        }

        foreach (string trigger in _castTriggers)
        {
            var clip = GetAnimationFromTrigger(trigger);
            if (clip != null)
            {
                _clipDurations.TryAdd(clip.name, clip.length);
            }
        }
    }
    #endregion

    #region Playing
    public void PlayTrigger(string triggerName, float castSpeed = float.MinValue)
    {
        if (castSpeed == float.MinValue)
            castSpeed = CastSpeed;
        //Debug.Log("CastSpeed" + castSpeed);

        int hash = Animator.StringToHash(triggerName);
        _animator.SetFloat(HashAnimPlayer.CastSpeed, castSpeed);
        _animator.SetTrigger(hash);
        _netAnimator.SetTrigger(hash);

        _activeTrigger = hash;
        _activeClip = GetAnimationFromTrigger(triggerName);
        if (_activeClip == null)
        {
            Debug.LogError($"Couldn't find clip for trigger {triggerName}");
            return;
        }
        _activeClipDuration = GetClipDuration(_activeClip.name);
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
    #endregion

    #region Caching
    /// <summary>
    /// Находит длительность анимации по названию
    /// Кэширует [clip]:duration в _clipDurations
    /// </summary>
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
        //Debug.Log($"animation length: {animation.length / CastSpeed}");
        _clipDurations.TryAdd(clipName, animation.length);
        return animation.length / CastSpeed;
    }

    /// <summary>
    /// Ищет в Animator 1 анимацию,
    /// которая запускается по триггеру.
    /// Кэширует связь [trigger]:clip в _triggerToClip
    /// Кэширует длительность [clip]:length в _clipDurations
    /// </summary>
    public AnimationClip GetAnimationFromTrigger(string trigger)
    {
        if (_triggerToClip.ContainsKey(trigger))
            return _triggerToClip[trigger];

#if UNITY_EDITOR
    AnimatorController controller = _animator.runtimeAnimatorController as AnimatorController;
    
    // Если используется Override Controller в редакторе:
    if (controller == null && _animator.runtimeAnimatorController is AnimatorOverrideController overrideController)
    {
        controller = overrideController.runtimeAnimatorController as AnimatorController;
    }

    if (controller != null)
    {
        foreach (var layer in controller.layers)
        {
            var allTransitions = layer.stateMachine.anyStateTransitions
                .Concat(layer.stateMachine.states.SelectMany(s => s.state.transitions));

            foreach (var transition in allTransitions)
            {
                foreach (var condition in transition.conditions)
                {
                    if (condition.parameter == trigger)
                    {
                        var clip = transition.destinationState.motion as AnimationClip;
                        if (clip != null)
                        {
                            _triggerToClip.TryAdd(trigger, clip);
                            _clipDurations.TryAdd(clip.name, clip.length);
                            return clip;
                        }
                    }
                }
            }
        }
    }
#else
        Debug.LogWarning("GetAnimationFromTrigger via AnimatorController is not supported in builds!");
#endif

        return null;
    }
    #endregion
    #endregion
}
