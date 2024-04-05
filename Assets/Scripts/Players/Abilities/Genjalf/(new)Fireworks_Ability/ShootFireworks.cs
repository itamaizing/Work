using System.Collections;
using GlobalEvents;
using UnityEngine;
using UnityEngine.UI;

public class ShootFireworks : Ability
{
    [Header("Ability settings")]
    [SerializeField] Fireworks _fireworksPref;
    [SerializeField] private float _manaCostPerSecond;
    [SerializeField] private float _duration;
    [Header("Size")]
    [SerializeField] private float _length;
    [SerializeField] private float _width;
    [SerializeField] private float _extraWidth;
    [SerializeField] private float _positionForExtraWidth;
    [Header("Damage")]
    [SerializeField] private float _damageRate;
    [SerializeField] private float _minDamagePerTick;
    [SerializeField] private float _maxDamagePerTick;
    [SerializeField] private float _percentFirstTarget;
    [SerializeField] private float _percentSecondTarget;
    [SerializeField] private float _percentThirdTarget;
    [SerializeField] private float _percentOtherTarget;

    private Fireworks _fireworks;
    private Coroutine _useJob;

    private void OnValidate()
    {
        if (_positionForExtraWidth > _length)
            _positionForExtraWidth = _length;
    }

    public override void Use()
    {
        if (IsReady)
        {
            IsReady = false;
            _useJob = StartCoroutine(UseCoroutine());
        }
    }

    private IEnumerator UseCoroutine()
    {
        _fireworks = Instantiate(_fireworksPref, transform);
        _fireworks.SetLength(_length);
        _fireworks.SetWidth(_width);
        _fireworks.SetPositionForExtraWidth(_positionForExtraWidth * 2);
        _fireworks.SetExtraWidth(_extraWidth);
        while (Input.GetMouseButtonDown(0) == false)
        {
            _fireworks.RotateAtMouse();
            yield return null;
        }
        _fireworks.Activate();

        RaycastHit2D rayHit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
        if (rayHit.transform.CompareTag("Enemies"))
        {
            _fireworks.SetTarget(rayHit.transform);
        }
        float time = 0;

        while (time < _duration)
        {
            time += Time.deltaTime;



            yield return null;
        }
    }
}