using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FillAmountOverTime : MonoBehaviour
{
    [SerializeField] private Image _image;

    private float _currentTime;
    private float _duration;
    private Coroutine _fillJob;

    public void StartFill(float duration, float startValue = 0, float endValue = 1, float curretTime = 0)
    {
        gameObject.SetActive(true);

        if(_fillJob != null)
        {
            _duration += -_currentTime + duration;
            StopCoroutine(_fillJob);
            _fillJob = StartCoroutine(ChangeFillAmountOverTimeCoroutine(_duration, curretTime, startValue, endValue));
        }
        else
        {
            _duration = duration;
            _fillJob = StartCoroutine(ChangeFillAmountOverTimeCoroutine(_duration, curretTime, startValue, endValue));
        }
    }

    IEnumerator ChangeFillAmountOverTimeCoroutine(float duration, float curretTime = 0, float startValue = 0, float endValue = 1)
    {
        while (curretTime < duration)
        {
            _image.fillAmount = Mathf.Lerp(startValue, endValue, curretTime / duration);
            curretTime += Time.deltaTime;
            _currentTime = curretTime;
            yield return null;
        }
        _fillJob = null;
        _image.fillAmount = endValue;
        gameObject.SetActive(false);
    }
}
