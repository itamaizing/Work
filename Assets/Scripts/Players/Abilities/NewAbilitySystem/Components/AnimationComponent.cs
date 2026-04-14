using Mirror;
using Org.BouncyCastle.Pkcs;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AnimationComponent : BaseSkillComponent
{
    #region InspectorFields

    #endregion

    #region RuntimeVariables
    private Animator _animator;
    private NetworkAnimator _netAnimator;
    public int _activeClip = 0;
    private float _activeClipDuration = -1;
    private Dictionary<int, float> animLengths = new();
    #endregion

    #region Properties
    public float CastSpeed
    {
        get
        {
            if (_skill.Info.AbilityForm == AbilityForm.Physical)
                return _skillAttributes.CastSpeedPhysical;
            else
                return _skillAttributes.CastSpeedMagical;
        }
    }

    public float ActiveClipDuration { get => _activeClipDuration; }
    #endregion

    #region Methods
    public override void Init(Skill skill)
    {
        base.Init(skill);
        _animator = _character.GetComponent<Animator>();
        _netAnimator = _character.GetComponent<NetworkAnimator>();
    }

    public void PlayAnimation(int clipHash, float castSpeed = float.MinValue)
    {
        if (castSpeed == float.MinValue)
            castSpeed = CastSpeed;
        _animator.SetFloat(HashAnimPlayer.AnimCancled, castSpeed);
        _animator.SetTrigger(clipHash);
        _netAnimator.SetTrigger(clipHash);

        _activeClip = clipHash;
        _activeClipDuration = GetDuration(clipHash);
    }

    public float GetDuration(int clipHash)
    {
        if (animLengths.TryGetValue(clipHash, out float duration))
            return duration / CastSpeed;

        AnimationClip animation = null;

        foreach (AnimationClip anim in _animator.runtimeAnimatorController.animationClips)
        {
            if (Animator.StringToHash(anim.name) == clipHash)
            {
                animation = anim;
                break;
            }
        }

        if (animation == null)
            return -1;

        Debug.Log($"animation length: {animation.length / CastSpeed}");
        animLengths.Add(clipHash, animation.length);
        return animation.length / CastSpeed;
    }

    public void Cancel()
    {
        ResetCurrent();

        _animator.SetTrigger(HashAnimPlayer.AnimCancled);
        _netAnimator.SetTrigger(HashAnimPlayer.AnimCancled);
    }

    public void ResetCurrent()
    {
        if (_activeClip == 0)
            return;
        _animator.ResetTrigger(_activeClip);
        _netAnimator.ResetTrigger(_activeClip);
        _activeClip = 0;
        _activeClipDuration = -1;
    }
    #endregion
}
