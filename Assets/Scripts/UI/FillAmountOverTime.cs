using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FillAmountOverTime : MonoBehaviour
{
    [SerializeField] private Image _image;

    public void StartFill(float duration, bool isReverse = false, float startValue = 0, float endValue = 1, float curretTime = 0)
    {
        gameObject.SetActive(true);

        if (isReverse)
            _image.fillOrigin = 1;
        else
            _image.fillOrigin = 0;

        StartCoroutine(ChangeFillAmountOverTimeCoroutine(duration, curretTime, startValue, endValue));
    }

    IEnumerator ChangeFillAmountOverTimeCoroutine(float duration, float curretTime = 0, float startValue = 0, float endValue = 1)
    {
        while (curretTime < duration)
        {
            _image.fillAmount = Mathf.Lerp(startValue, endValue, curretTime / duration);
            curretTime += Time.deltaTime;
            yield return null;
        }
        _image.fillAmount = endValue;
        gameObject.SetActive(false);
    }
}
