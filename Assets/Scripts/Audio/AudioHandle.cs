using System;
using System.Collections;
using UnityEngine;

public class AudioHandle : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;

    public bool IsInUse { get; private set; }
    public event Action<AudioHandle> Released;

    private Transform _followTarget;
    private bool _hasFollowTarget;
    private Coroutine _autoReleaseJob;

    public void Play(AudioData data, AudioAssetSO asset, Vector3? position, Transform followTarget)
    {
        IsInUse = true;
        gameObject.SetActive(true);

        AudioHandleUtility.ApplyAudioData(_audioSource, data, asset);

        _followTarget = followTarget;
        _hasFollowTarget = followTarget != null;

        if (_hasFollowTarget)
        {
            _audioSource.spatialBlend = 1f;
            transform.position = followTarget.position;
        }
        else if (position.HasValue)
        {
            _audioSource.spatialBlend = 1f;
            transform.position = position.Value;
        }
        else
        {
            _audioSource.spatialBlend = 0f;
        }

        _audioSource.Play();

        if (data.PlayMode == SfxPlayMode.Once)
            _autoReleaseJob = StartCoroutine(AutoReleaseAfterClip());
    }

    private void LateUpdate()
    {
        if (!IsInUse || !_hasFollowTarget) return;

        if (_followTarget == null)
        {
            Stop();
            return;
        }

        transform.position = _followTarget.position;
    }

    private IEnumerator AutoReleaseAfterClip()
    {
        yield return new WaitForSeconds(_audioSource.clip.length / Mathf.Max(0.01f, _audioSource.pitch));
        _autoReleaseJob = null;
        Stop();
    }

    public void Stop()
    {
        if (!IsInUse) return;
        _audioSource.Stop();
        Release();
    }

    private void Release()
    {
        if (_autoReleaseJob != null) StopCoroutine(_autoReleaseJob);
        _autoReleaseJob = null;

        IsInUse = false;
        _followTarget = null;
        _hasFollowTarget = false;
        _audioSource.clip = null;
        gameObject.SetActive(false);

        Released?.Invoke(this);
    }
}