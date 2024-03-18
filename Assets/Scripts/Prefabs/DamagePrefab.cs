using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DamagePrefab : MonoBehaviour
{
    private TextMeshPro _textMeshPro;
    private float _speed = 2;
    private float _duration = 0.75f;
    private float _startTime;
    public Color StartColor;
    public Color EndColor;

    private void Start()
    {
        _textMeshPro = GetComponent<TextMeshPro>();
        _startTime = Time.time;
    }

    void Update()
    {
        transform.position += Vector3.up * Time.deltaTime * _speed;

        float elapsedTime = Time.time - _startTime;
        if (elapsedTime < _duration)
        {
            float lerpValue = elapsedTime / _duration;
            _textMeshPro.color = Color.Lerp(StartColor, EndColor, lerpValue);
        }

        if (elapsedTime >= _duration)
        {
            Destroy(gameObject);
        }
    }
}







