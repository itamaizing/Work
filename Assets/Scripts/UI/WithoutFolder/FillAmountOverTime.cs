using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FillAmountOverTime : MonoBehaviour
{
    [SerializeField] private Image _image;

    private int _defaultFillOrigin;
    private float _currentTime;
    private float _duration;
    private Coroutine _fillJob;

    private float _startValue;
    private float _endValue;

    public event Action<FillAmountOverTime> Ended;

    private void Awake()
    {
        _defaultFillOrigin = _image.fillOrigin;
    }

    public void Stop()
    {
        if (_fillJob != null)
        {
            StopCoroutine(_fillJob);
            _fillJob = null;
        }
        gameObject.SetActive(false);
    }

    public void StartFill(float duration, float startValue = 0, float endValue = 1, 
                         bool addTime = true, float currentTime = 0, int type = -1)
    {
        gameObject.SetActive(true);

        if (type >= 0)
            _image.fillOrigin = type;
        else
            _image.fillOrigin = _defaultFillOrigin;

        if (_fillJob != null)
            StopCoroutine(_fillJob);

        if (addTime && _fillJob != null)
            _duration += duration;
        else
            _duration = duration;

        _startValue = startValue;
        _endValue = endValue;

        _fillJob = StartCoroutine(FillCoroutine(duration, currentTime, startValue, endValue));
    }

    private IEnumerator FillCoroutine(float duration, float currentTime, float startValue, float endValue)
    {
        _currentTime = Mathf.Clamp(currentTime, 0, duration);

        while (_currentTime < duration)
        {
            float progress = _currentTime / duration;
            _image.fillAmount = Mathf.Lerp(startValue, endValue, progress);

            _currentTime += Time.deltaTime;
            yield return null;
        }

        _image.fillAmount = endValue;
        _fillJob = null;
        Ended?.Invoke(this);
        gameObject.SetActive(false);
    }
    
    public void Rollback(float timeToRollback)
    {
        if (_fillJob == null || timeToRollback <= 0f) return;
        
        StopCoroutine(_fillJob);

        _currentTime = Mathf.Max(0f, _currentTime - timeToRollback);

        _fillJob = StartCoroutine(FillCoroutine(_duration, _currentTime, _startValue, _endValue));
    }
    
    public void SkipForward(float timeToSkip)
    {
        if (_fillJob == null || timeToSkip <= 0f) return;

        StopCoroutine(_fillJob);
        _currentTime = Mathf.Min(_duration, _currentTime + timeToSkip);
        _fillJob = StartCoroutine(FillCoroutine(_duration, _currentTime, _startValue, _endValue));
    }

    public float CurrentProgress => _duration > 0 ? _currentTime / _duration : 0f;
}