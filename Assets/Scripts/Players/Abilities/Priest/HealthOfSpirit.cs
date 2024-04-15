using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class HealthOfSpirit : MonoBehaviour
{
    public event Action<HealthOfSpirit> Destroyed;
    private Coroutine _coroutine;

    public void StartCountdownCoroutine(float time)
    {
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
        }
        _coroutine = StartCoroutine(CountdownRoutine(time));
    }

    public IEnumerator CountdownRoutine(float time)
    {
        while (time > 0)
        {
            yield return new WaitForSeconds(1f);
            time--;
        }
        Destroy(this);
    }

    private void OnDestroy()
    {
        Destroyed?.Invoke(this);
    }
}
