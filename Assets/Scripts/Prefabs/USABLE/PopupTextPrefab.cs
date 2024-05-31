using TMPro;
using UnityEngine;

public class PopupTextPrefab : MonoBehaviour
{
    [SerializeField]
    private TextMeshPro _popupText;
    [SerializeField]
    private float _speed = 2;
    [SerializeField]
    private float _duration = 0.75f;
    
    private float _startTime;
    public TextMeshPro PopupText =>_popupText;
    public Color StartColor;
    public Color EndColor;

    private void Awake()
    {
        _startTime = Time.time;
    }

    void Update()
    {
        transform.position += Vector3.up * (Time.deltaTime * _speed);

        float elapsedTime = Time.time - _startTime;
        if (elapsedTime < _duration)
        {
            float lerpValue = elapsedTime / _duration;
            _popupText.color = Color.Lerp(StartColor, EndColor, lerpValue);
        }

        if (elapsedTime >= _duration)
        {
            Destroy(gameObject);
        }
    }
}







